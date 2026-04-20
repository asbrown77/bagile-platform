using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bagile.Application.Common.Interfaces;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Microsoft.Extensions.Logging;

namespace Bagile.Infrastructure.Services;

public class WooCommercePublishService : IWooCommercePublishService
{
    private readonly IWooApiClient _wooClient;
    private readonly ILogger<WooCommercePublishService> _logger;

    // Course type code → full product name prefix (before the date part)
    // The trademark symbol \u2122 appears in WooCommerce product names
    private static readonly Dictionary<string, string> CourseTypeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PSM"]    = "Professional Scrum Master\u2122",
        ["PSPO"]   = "Professional Scrum Product Owner\u2122",
        ["PSK"]    = "Professional Scrum with Kanban",
        ["PALE"]   = "Professional Agile Leadership\u2122 - Essentials",
        ["EBM"]    = "Professional Agile Leadership\u2122 - Evidence Based Management",
        ["APSSD"]  = "Applying Professional Scrum\u2122 for Software Development",
        ["APS"]    = "Applying Professional Scrum\u2122",
        ["PSMA"]   = "Professional Scrum Master - Advanced\u2122",
        ["PSPOA"]  = "Professional Scrum Product Owner - Advanced\u2122",
        ["PSMAI"]  = "Professional Scrum Master\u2122 - AI Essentials",
        ["PSPOAI"] = "Professional Scrum Product Owner\u2122 - AI Essentials",
        ["PSFS"]   = "Professional Scrum Facilitation Skills\u2122",
    };

    // Trainer name → initials
    private static readonly Dictionary<string, string> TrainerInitials = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alex Brown"] = "AB",
        ["Chris Bexon"] = "CB",
    };

    // Trainer name → Zoom host ID
    private static readonly Dictionary<string, string> TrainerZoomHostIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alex Brown"] = "Tv1arteKSKeEOCqpYpdqvQ",
        ["Chris Bexon"] = "bNy27xgkQhymtSsYnUZRmQ",
    };

    // Trainer name → Zoom recurring meeting/webinar ID (used for WooCommerceEventsZoomWebinar)
    private static readonly Dictionary<string, string> TrainerZoomMeetingIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alex Brown"] = "85023208940_meetings",
        ["Chris Bexon"] = "84523846342_meetings",
    };

    // Trainer name → WooCommerce user ID for select_a_trainer field
    private static readonly Dictionary<string, string> TrainerWooUserIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Alex Brown"] = "383",
        ["Chris Bexon"] = "380",
    };

    // Fallback template course type when no product exists for the exact type.
    // Keys: course type with no live/draft product. Values: fallback type to search instead.
    private static readonly Dictionary<string, string> CourseTypeFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PSPOA"]  = "PSPO",   // PSPO-A → PSPO (same family)
        ["PSMA"]   = "PSM",    // PSM-A → PSM (same family)
        ["PALEBM"] = "PALE",   // PAL-EBM → PAL-E (closest structure)
    };

    // Authoritative GBP prices per course type. Used when falling back to a different template
    // type so we don't inherit the wrong price.
    private static readonly Dictionary<string, string> CourseTypePrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PSM"]    = "950",
        ["PSPO"]   = "950",
        ["PSK"]    = "950",
        ["PALE"]   = "995",
        ["PSMA"]   = "1050",
        ["PSPOA"]  = "1095",
        ["PSMAI"]  = "550",
        ["PSPOAI"] = "550",
        ["EBM"]    = "595",
        ["PALEBM"] = "595",
        ["PSFS"]   = "595",
        ["APS"]    = "595",
        ["APSSD"]  = "1495",
    };

    // Course type → featured image ID (from live WooCommerce products, verified 14 Apr 2026)
    private static readonly Dictionary<string, int> CourseImageIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PSM"]    = 98,     // Professional Scrum Master
        ["PSPO"]   = 93,     // Scrumorg-PSPO
        ["PSK"]    = 107,    // Scrumorg-PSK
        ["PALE"]   = 10962,  // PALE 400
        ["EBM"]    = 3442,   // PALEBM 600
        ["APS"]    = 91,     // Scrumorg-APS
        ["APSSD"]  = 106,    // Scrumorg-APS-SD
        ["PSMA"]   = 101,    // Scrumorg-PSMA
        ["PSPOA"]  = 105,    // Scrumorg-PSPOA
        ["PSMAI"]  = 12743,  // PSM-AIE-600
        ["PSPOAI"] = 12205,  // PSPO-AI-Essentials 600
        ["PSFS"]   = 3443,   // PSFS 600
        ["PSU"]    = 3648,   // PSU-400
    };

    public WooCommercePublishService(IWooApiClient wooClient, ILogger<WooCommercePublishService> logger)
    {
        _wooClient = wooClient;
        _logger = logger;
    }

    public async Task<WooPublishResult?> CreateProductAsync(WooPublishRequest request, CancellationToken ct = default)
    {
        // 1. Find the most recent product of the same course type to use as template
        var templateProduct = await FindTemplateProductAsync(request.CourseType, request.TrainerName, ct);
        if (templateProduct == null)
        {
            _logger.LogError("No template product found for course type {CourseType}", request.CourseType);
            return null;
        }

        _logger.LogInformation("Using template product {ProductId} for {CourseType}",
            templateProduct.RootElement.GetProperty("id").GetInt64(),
            request.CourseType);

        // 2. Build the new product payload
        var payload = await BuildProductPayloadAsync(templateProduct, request, ct);

        // 3. Create the product via WooCommerce API
        var created = await _wooClient.CreateProductAsync(payload, ct);
        if (created == null)
        {
            _logger.LogError("WooCommerce product creation returned null");
            return null;
        }

        var productId = created.RootElement.GetProperty("id").GetInt64();
        var permalink = created.RootElement.GetProperty("permalink").GetString() ?? "";

        _logger.LogInformation("Created WooCommerce product {ProductId}: {Url}", productId, permalink);

        // Sanity check: re-fetch and verify critical fields landed correctly
        var warnings = await VerifyProductAsync(productId, request, ct);
        if (warnings.Count > 0)
            _logger.LogWarning("Product {ProductId} created with warnings: {Warnings}", productId, string.Join("; ", warnings));

        return new WooPublishResult
        {
            ProductId = productId,
            ProductUrl = permalink,
            Warnings = warnings
        };
    }

    public async Task<string?> FindTemplateSkuAsync(string courseType, string? trainerName = null, CancellationToken ct = default)
    {
        var searchOrder = new List<string> { courseType.ToUpperInvariant() };
        if (CourseTypeFallbacks.TryGetValue(courseType, out var fallback))
            searchOrder.Add(fallback.ToUpperInvariant());

        foreach (var typeToSearch in searchOrder)
        {
            var match = await SearchTemplateByPrefixAsync(typeToSearch + "-", trainerName, ct);
            if (match?.Sku != null) return match.Sku;
        }

        return null;
    }

    public async Task<bool> UpdateProductMetaAsync(
        long productId,
        Dictionary<string, string> metaUpdates,
        CancellationToken ct = default)
    {
        var metaArray = metaUpdates.Select(kv => new { key = kv.Key, value = kv.Value }).ToArray();
        var payloadJson = JsonSerializer.Serialize(new { meta_data = metaArray });
        var payload = JsonDocument.Parse(payloadJson).RootElement;

        var result = await _wooClient.UpdateProductAsync(productId, payload, ct);
        return result != null;
    }

    private async Task<IReadOnlyList<string>> VerifyProductAsync(long productId, WooPublishRequest request, CancellationToken ct)
    {
        var warnings = new List<string>();
        try
        {
            var product = await _wooClient.GetProductFullAsync(productId, ct);
            if (product == null)
            {
                warnings.Add("Could not re-fetch product for sanity check");
                return warnings;
            }

            var root = product.RootElement;
            var initials = GetTrainerInitials(request.TrainerName);
            var dateCode = request.StartDate.ToString("ddMMyy");
            var expectedSku = $"{request.CourseType.ToUpperInvariant()}-{dateCode}-{initials}";
            var expectedMenuOrder = int.Parse(request.StartDate.ToString("yyyyMMdd"));
            var expectedStartDate = request.StartDate.ToString("yyyy-MM-dd");

            // Check SKU
            var actualSku = GetStringProp(root, "sku");
            if (!string.Equals(actualSku, expectedSku, StringComparison.OrdinalIgnoreCase))
                warnings.Add($"SKU mismatch: expected {expectedSku}, got '{actualSku}'");

            // Check price
            var price = GetStringProp(root, "regular_price");
            if (string.IsNullOrEmpty(price) || price == "0" || price == "0.00")
                warnings.Add($"Price is missing or zero — check template product has a price set");

            // Check menu_order
            if (root.TryGetProperty("menu_order", out var menuOrderEl))
            {
                var actualOrder = menuOrderEl.GetInt32();
                if (actualOrder != expectedMenuOrder)
                    warnings.Add($"menu_order mismatch: expected {expectedMenuOrder}, got {actualOrder}");
            }
            else
            {
                warnings.Add("menu_order field missing from product");
            }

            // Check FooEvents start date
            if (root.TryGetProperty("meta_data", out var metaData))
            {
                var fooDate = metaData.EnumerateArray()
                    .FirstOrDefault(m => m.GetProperty("key").GetString() == "WooCommerceEventsDate");
                if (fooDate.ValueKind != JsonValueKind.Undefined)
                {
                    var actualDate = fooDate.GetProperty("value").GetString() ?? "";
                    if (actualDate != expectedStartDate)
                        warnings.Add($"FooEvents date mismatch: expected {expectedStartDate}, got '{actualDate}'");
                }
                else
                {
                    warnings.Add("WooCommerceEventsDate meta field missing — FooEvents may not show the event");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sanity check failed for product {ProductId}", productId);
            warnings.Add($"Sanity check error: {ex.Message}");
        }

        return warnings;
    }

    private async Task<JsonDocument?> FindTemplateProductAsync(string courseType, string? trainerName, CancellationToken ct)
    {
        // Try to find a product for the exact course type, then fall back to a related type.
        // Build the search order: exact type first, then fallback if defined.
        var searchOrder = new List<string> { courseType.ToUpperInvariant() };
        if (CourseTypeFallbacks.TryGetValue(courseType, out var fallback))
            searchOrder.Add(fallback.ToUpperInvariant());

        foreach (var typeToSearch in searchOrder)
        {
            var skuPrefix = typeToSearch + "-";
            var template = await SearchTemplateByPrefixAsync(skuPrefix, trainerName, ct);
            if (template != null)
            {
                if (!typeToSearch.Equals(courseType, StringComparison.OrdinalIgnoreCase))
                    _logger.LogInformation("Using {FallbackType} as template for {CourseType} (no exact match found)", typeToSearch, courseType);
                return await _wooClient.GetProductFullAsync(template.Id, ct);
            }
        }

        _logger.LogWarning("No template product found for course type {CourseType} (tried: {Searched})",
            courseType, string.Join(", ", searchOrder));
        return null;
    }

    private async Task<WooProductDto?> SearchTemplateByPrefixAsync(string skuPrefix, string? trainerName, CancellationToken ct)
    {
        // WooCommerce full-text search is unreliable for SKU-based lookup (search index may be stale).
        // Use direct product listing instead: fetch the 100 most recent products per status and
        // filter by SKU prefix client-side. For a small shop (< 100 active products per status)
        // this reliably covers all candidates.
        //
        // When a trainer name is provided, prefer the product whose SKU ends with the trainer's
        // initials (e.g. "-AB") before falling back to the highest-id product of any trainer.
        var trainerInitials = trainerName != null ? GetTrainerInitials(trainerName) : null;

        foreach (var status in new[] { "publish", "draft" })
        {
            var products = await _wooClient.ListProductsByStatusAsync(status: status, perPage: 100, ct: ct);
            var candidates = products
                .Where(p => p.Sku != null && p.Sku.StartsWith(skuPrefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (candidates.Count == 0) continue;

            // Trainer-preferred: look for a SKU ending with "-{initials}" first
            if (trainerInitials != null)
            {
                var suffix = $"-{trainerInitials}";
                var preferred = candidates
                    .Where(p => p.Sku!.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefault();
                if (preferred != null) return preferred;
            }

            // Fall back to highest id of any trainer
            return candidates.OrderByDescending(p => p.Id).First();
        }
        return null;
    }

    private async Task<JsonElement> BuildProductPayloadAsync(JsonDocument template, WooPublishRequest request, CancellationToken ct)
    {
        var root = template.RootElement;
        var initials = GetTrainerInitials(request.TrainerName);
        var dateCode = request.StartDate.ToString("ddMMyy");
        var slug = $"{request.CourseType.ToLowerInvariant()}-{dateCode}";
        var sku = $"{request.CourseType.ToUpperInvariant()}-{dateCode}-{initials}";

        var courseName = GetCourseName(request.CourseType);
        var dateRange = FormatDateRange(request.StartDate, request.EndDate);
        var productName = $"{courseName} - {dateRange}";

        // Calculate number of days
        var numDays = (int)(request.EndDate - request.StartDate).TotalDays + 1;

        // Build meta_data array from template, overriding date/event fields
        var metaData = BuildMetaData(root, request, sku, numDays);

        // Build categories and tags from template
        var categories = CloneJsonArray(root, "categories");
        var tags = await BuildTagsAsync(root, request, ct);
        var images = BuildImages(request.CourseType, root);

        // Price: use our authoritative price map if known; otherwise copy from template.
        // This prevents inheriting the wrong price when using a fallback template type.
        var price = CourseTypePrices.TryGetValue(request.CourseType, out var knownPrice)
            ? knownPrice
            : GetStringProp(root, "regular_price");

        var payload = new Dictionary<string, object?>
        {
            ["name"] = productName,
            ["slug"] = slug,
            ["sku"] = sku,
            ["type"] = "simple",
            ["status"] = "draft",
            ["menu_order"] = int.Parse(request.StartDate.ToString("yyyyMMdd")),
            ["catalog_visibility"] = "visible",
            ["description"] = GetStringProp(root, "description"),
            ["short_description"] = GetStringProp(root, "short_description"),
            ["regular_price"] = price,
            ["manage_stock"] = false,
            ["stock_status"] = "instock",
            ["categories"] = categories,
            ["tags"] = tags,
            ["images"] = images,
            ["meta_data"] = metaData
        };

        var json = JsonSerializer.Serialize(payload);
        return JsonDocument.Parse(json).RootElement;
    }

    private List<object> BuildMetaData(JsonElement root, WooPublishRequest request, string sku, int numDays)
    {
        var meta = new List<object>();

        // Copy all meta_data from template, overriding specific keys
        var overrides = BuildMetaOverrides(request, numDays);

        // Keys to skip entirely (product-specific, will be set fresh)
        var skipKeys = new HashSet<string>
        {
            "_edit_lock", "_edit_last", "_wp_old_slug",
            "woopt_actions" // Expiration actions are product-specific
        };

        if (root.TryGetProperty("meta_data", out var templateMeta))
        {
            foreach (var m in templateMeta.EnumerateArray())
            {
                var key = m.GetProperty("key").GetString() ?? "";

                if (skipKeys.Contains(key))
                    continue;

                if (overrides.TryGetValue(key, out var overrideValue))
                {
                    meta.Add(new { key, value = overrideValue });
                    overrides.Remove(key); // Mark as used
                }
                else
                {
                    // Copy as-is from template
                    var value = m.GetProperty("value");
                    meta.Add(new { key, value = GetMetaValue(value) });
                }
            }
        }

        // Add any overrides that weren't in the template (new fields)
        foreach (var kv in overrides)
        {
            meta.Add(new { key = kv.Key, value = kv.Value });
        }

        // PSM-A specific: fix ticket text to use PSM-A instead of PSM II
        if (request.CourseType.Equals("PSMA", StringComparison.OrdinalIgnoreCase))
        {
            FixPsmaTicketText(meta);
        }

        return meta;
    }

    private Dictionary<string, object> BuildMetaOverrides(WooPublishRequest request, int numDays)
    {
        var startDateStr = request.StartDate.ToString("yyyyMMdd");
        var endDateStr = request.EndDate.ToString("yyyyMMdd");
        var startMysql = request.StartDate.ToString("yyyy-MM-dd") + " 00:00:00";
        var endMysql = request.EndDate.ToString("yyyy-MM-dd") + " 00:00:00";
        var startTimestamp = new DateTimeOffset(request.StartDate, TimeSpan.Zero).ToUnixTimeSeconds().ToString();
        var endTimestamp = new DateTimeOffset(request.EndDate, TimeSpan.Zero).ToUnixTimeSeconds().ToString();
        var endExpireTimestamp = new DateTimeOffset(request.EndDate, TimeSpan.Zero).ToUnixTimeSeconds();

        var zoomHostId = GetTrainerZoomHostId(request.TrainerName);
        TrainerZoomMeetingIds.TryGetValue(request.TrainerName, out var zoomMeetingId);
        TrainerWooUserIds.TryGetValue(request.TrainerName, out var trainerWooUserId);
        var location = request.IsVirtual ? "Live virtual training" : (request.Venue ?? "");

        // Use display-friendly names for MailChimp tags (matches segment filters)
        var mailchimpDisplayType = request.CourseType.ToUpperInvariant() switch
        {
            "PSMA"  => "PSM-A",
            "PSPOA" => "PSPO-A",
            var ct  => ct,
        };
        var mailchimpTags = $"Public, Student, {mailchimpDisplayType}";

        return new Dictionary<string, object>
        {
            // ACF date fields
            ["event_date"] = startDateStr,
            ["event_end_date"] = endDateStr,

            // FooEvents core date fields
            ["WooCommerceEventsDate"] = request.StartDate.ToString("yyyy-MM-dd"),
            ["WooCommerceEventsDateTimestamp"] = startTimestamp,
            ["WooCommerceEventsDateTimeTimestamp"] = startTimestamp,
            ["WooCommerceEventsEndDate"] = request.EndDate.ToString("yyyy-MM-dd"),
            ["WooCommerceEventsEndDateTimestamp"] = endTimestamp,
            ["WooCommerceEventsEndDateTimeTimestamp"] = endTimestamp,
            ["WooCommerceEventsDateMySQLFormat"] = startMysql,
            ["WooCommerceEventsEndDateMySQLFormat"] = endMysql,
            ["WooCommerceEventsNumDays"] = numDays.ToString(),
            // Expire on end date (not start date) so multi-day courses don't close sales on Day 1
            ["WooCommerceEventsExpire"] = request.EndDate.ToString("yyyy-MM-dd") + " 00:00",
            ["WooCommerceEventsExpireTimestamp"] = endTimestamp,

            // Location
            ["WooCommerceEventsLocation"] = location,
            ["event_type"] = request.IsVirtual ? "Live virtual training" : "Face to Face",

            // Zoom — host and recurring meeting ID are trainer-specific
            ["WooCommerceEventsZoomHost"] = zoomHostId,
            ["WooCommerceEventsZoomWebinar"] = zoomMeetingId ?? "auto",

            // Expiration (product expires on end date → changes to draft)
            ["_expiration-date"] = endExpireTimestamp,
            ["_expiration-date-status"] = "saved",
            ["_expiration-date-type"] = "change-status",
            ["_expiration-date-post-status"] = "draft",

            // MailChimp
            ["WooCommerceEventsMailchimpTags"] = mailchimpTags,

            // Trainer selector — ACF expects PHP array notation with single quotes, e.g. ['383']
            // NOT JSON double-quote format ["383"] — ACF reads the raw stored string and won't match it
            ["select_a_trainer"] = trainerWooUserId != null ? $"['{trainerWooUserId}']" : "[]",
        };
    }

    private static void FixPsmaTicketText(List<object> meta)
    {
        // Find the ticket text meta entry and fix PSM II references
        for (int i = 0; i < meta.Count; i++)
        {
            var item = meta[i];
            var type = item.GetType();
            var keyProp = type.GetProperty("key");
            var valueProp = type.GetProperty("value");

            if (keyProp == null || valueProp == null) continue;

            var key = keyProp.GetValue(item)?.ToString();
            if (key != "WooCommerceEventsTicketText") continue;

            var value = valueProp.GetValue(item)?.ToString();
            if (string.IsNullOrEmpty(value)) continue;

            // Replace "PSM II" with "PSM-A" and "Professional Scrum Master II" with
            // "Professional Scrum Master - Advanced" using regex for non-breaking spaces
            value = Regex.Replace(value, @"PSM[\s\xa0]+II", "PSM-A");
            value = Regex.Replace(value, @"Professional[\s\xa0]+Scrum[\s\xa0]+Master[\s\xa0]+II",
                "Professional Scrum Master - Advanced");

            // Rebuild the anonymous object with the fixed value
            meta[i] = new { key, value };
            break;
        }
    }

    private static string GetCourseName(string courseType)
    {
        if (CourseTypeNames.TryGetValue(courseType, out var name))
            return name;

        return courseType; // Fallback to the raw code
    }

    private static string GetTrainerInitials(string trainerName)
    {
        if (TrainerInitials.TryGetValue(trainerName, out var initials))
            return initials;

        // Fallback: first letter of each word
        var parts = trainerName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])));
    }

    private static string GetTrainerZoomHostId(string trainerName)
    {
        if (TrainerZoomHostIds.TryGetValue(trainerName, out var hostId))
            return hostId;

        return ""; // Unknown trainer — leave blank
    }

    private static string FormatDateRange(DateTime start, DateTime end)
    {
        if (start.Date == end.Date)
        {
            // Single day: "28 May 26"
            return start.ToString("d MMM yy", CultureInfo.InvariantCulture);
        }

        if (start.Month == end.Month && start.Year == end.Year)
        {
            // Same month: "28-29 May 26"
            return $"{start.Day}-{end.Day} {start.ToString("MMM yy", CultureInfo.InvariantCulture)}";
        }

        // Different months: "28 May - 1 Jun 26"
        return $"{start.ToString("d MMM", CultureInfo.InvariantCulture)} - {end.ToString("d MMM yy", CultureInfo.InvariantCulture)}";
    }

    private static List<object> CloneJsonArray(JsonElement root, string propertyName)
    {
        var result = new List<object>();
        if (!root.TryGetProperty(propertyName, out var array)) return result;

        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty("id", out var id))
            {
                result.Add(new { id = id.GetInt64() });
            }
        }
        return result;
    }

    private async Task<List<object>> BuildTagsAsync(JsonElement root, WooPublishRequest request, CancellationToken ct)
    {
        var tags = new List<object>();

        // Fetch all existing tags so we can resolve slugs to IDs
        var allTags = await _wooClient.GetAllTagsAsync(ct);
        var tagsBySlug = allTags.ToDictionary(t => t.Slug, t => t.Id, StringComparer.OrdinalIgnoreCase);

        // Copy existing tags from template, skipping month and trainer tags
        var monthSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "january", "february", "march", "april", "may", "june",
            "july", "august", "september", "october", "november", "december",
            "jan", "feb", "mar", "apr", "jun", "jul", "aug", "sep", "oct", "nov", "dec"
        };

        if (root.TryGetProperty("tags", out var tagsArray))
        {
            foreach (var tag in tagsArray.EnumerateArray())
            {
                var slug = tag.GetProperty("slug").GetString() ?? "";

                if (monthSlugs.Contains(slug))
                    continue;
                if (slug.Equals("alex-brown", StringComparison.OrdinalIgnoreCase) ||
                    slug.Equals("chris-bexon", StringComparison.OrdinalIgnoreCase))
                    continue;

                tags.Add(new { id = tag.GetProperty("id").GetInt64() });
            }
        }

        // Add trainer, month, and format tags by resolved ID
        var trainerSlug = request.TrainerName.ToLowerInvariant().Replace(" ", "-");
        if (tagsBySlug.TryGetValue(trainerSlug, out var trainerId))
            tags.Add(new { id = trainerId });

        var monthSlug = request.StartDate.ToString("MMMM", CultureInfo.InvariantCulture).ToLowerInvariant();
        if (tagsBySlug.TryGetValue(monthSlug, out var monthId))
            tags.Add(new { id = monthId });

        var formatSlug = request.IsVirtual ? "virtual-training" : "in-person";
        if (tagsBySlug.TryGetValue(formatSlug, out var formatId))
            tags.Add(new { id = formatId });

        return tags;
    }

    private static List<object> BuildImages(string courseType, JsonElement root)
    {
        // Use the known image ID for this course type, falling back to template's image
        if (CourseImageIds.TryGetValue(courseType, out var imageId))
        {
            return new List<object> { new { id = imageId } };
        }

        // Fallback: copy from template
        if (root.TryGetProperty("images", out var images) && images.GetArrayLength() > 0)
        {
            var firstImage = images[0];
            if (firstImage.TryGetProperty("id", out var id))
            {
                return new List<object> { new { id = id.GetInt64() } };
            }
        }

        return new List<object>();
    }

    private static string GetStringProp(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
            return prop.GetString() ?? "";
        return "";
    }

    private static object? GetMetaValue(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => value.GetRawText() // For arrays/objects, pass the raw JSON
        };
    }
}
