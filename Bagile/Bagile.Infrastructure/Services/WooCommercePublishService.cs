using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Bagile.Application.Common.Interfaces;
using Bagile.Infrastructure.Clients;
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
        var templateProduct = await FindTemplateProductAsync(request.CourseType, ct);
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

        return new WooPublishResult
        {
            ProductId = productId,
            ProductUrl = permalink
        };
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

    private async Task<JsonDocument?> FindTemplateProductAsync(string courseType, CancellationToken ct)
    {
        // Search by course type prefix in SKU (most reliable)
        var skuPrefix = courseType.ToUpperInvariant() + "-";
        var products = await _wooClient.SearchProductsAsync(skuPrefix, perPage: 5, status: "publish", ct: ct);

        // Find one whose SKU starts with our prefix
        var template = products
            .Where(p => p.Sku != null && p.Sku.StartsWith(skuPrefix, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Id) // Most recent by ID
            .FirstOrDefault();

        if (template == null)
        {
            // Try draft products too
            products = await _wooClient.SearchProductsAsync(skuPrefix, perPage: 5, status: "draft", ct: ct);
            template = products
                .Where(p => p.Sku != null && p.Sku.StartsWith(skuPrefix, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.Id)
                .FirstOrDefault();
        }

        if (template == null)
        {
            _logger.LogWarning("No template product found with SKU prefix {Prefix}", skuPrefix);
            return null;
        }

        // Fetch the full product with all meta_data
        return await _wooClient.GetProductFullAsync(template.Id, ct);
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

        // Price fields — copy from template
        var price = GetStringProp(root, "regular_price");

        var payload = new Dictionary<string, object?>
        {
            ["name"] = productName,
            ["slug"] = slug,
            ["sku"] = sku,
            ["type"] = "simple",
            ["status"] = "draft",
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
        var location = request.IsVirtual ? "Live virtual training" : (request.Venue ?? "");

        var mailchimpTags = request.CourseType.Equals("PSMA", StringComparison.OrdinalIgnoreCase)
            ? "Public, Student, PSM-A"
            : $"Public, Student, {request.CourseType.ToUpperInvariant()}";

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
            ["WooCommerceEventsExpire"] = request.StartDate.ToString("yyyy-MM-dd") + " 00:00",
            ["WooCommerceEventsExpireTimestamp"] = startTimestamp,

            // Location
            ["WooCommerceEventsLocation"] = location,
            ["event_type"] = request.IsVirtual ? "Live virtual training" : "Face to Face",

            // Zoom
            ["WooCommerceEventsZoomHost"] = zoomHostId,
            ["WooCommerceEventsZoomWebinar"] = "auto",

            // Expiration (product expires on end date → changes to draft)
            ["_expiration-date"] = endExpireTimestamp,
            ["_expiration-date-status"] = "saved",
            ["_expiration-date-type"] = "change-status",
            ["_expiration-date-post-status"] = "draft",

            // MailChimp
            ["WooCommerceEventsMailchimpTags"] = mailchimpTags,
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
