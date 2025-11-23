using System.Text.Json;
using Bagile.Domain.Entities;
using Bagile.EtlService.Mappers;
using Bagile.EtlService.Models;
using Bagile.Infrastructure.Clients;
using Microsoft.Extensions.Logging;
using System.Globalization;
using Bagile.EtlService.Helpers;

namespace Bagile.EtlService.Services
{
    public class WooOrderParser : IParser<CanonicalWooOrderDto>
    {
        private readonly ILogger<WooOrderParser> _logger;
        private readonly IFooEventsTicketsClient _fooEvents;

        public WooOrderParser(
            ILogger<WooOrderParser> logger,
            IFooEventsTicketsClient fooEvents)
        {
            _logger = logger;
            _fooEvents = fooEvents;
        }

        public async Task<CanonicalWooOrderDto> Parse(RawOrder raw)
        {
            using var doc = JsonDocument.Parse(raw.Payload);
            var root = doc.RootElement;

            int wooId = 0;
            if (root.TryGetProperty("id", out var idProp) &&
                idProp.TryGetInt32(out var numId))
            {
                wooId = numId;
            }

            var billing = root.GetProperty("billing");

            string billingEmail = SafeGet(billing, "email");
            string billingFirst = SafeGet(billing, "first_name");
            string billingLast = SafeGet(billing, "last_name");
            string billingCompany = SafeGet(billing, "company");

            var pluginProductId = ExtractFooEventsProductId(root);

            // STEP 1: Build tickets from line_items (with fallback SKU + date parsing)
            var baseTickets = BuildTicketsFromLineItems(
                root,
                billingFirst,
                billingLast,
                billingEmail,
                billingCompany,
                pluginProductId
            );

            // STEP 2: legacy ticket metadata
            bool hasLegacyMetadata = false;
            bool hasPluginMetadata = false;

            var legacyTickets = WooOrderTicketMapper.MapTickets(raw.Payload).ToList();
            if (legacyTickets.Count > 0)
            {
                hasLegacyMetadata = true;
                ApplyLegacyTicketData(baseTickets, legacyTickets, billingFirst, billingLast, billingEmail, billingCompany);
            }
            else
            {
                // STEP 3: FooEvents API fallback
                hasPluginMetadata = await TryApplyFooEventsPluginAsync(
                    wooId,
                    baseTickets,
                    billingFirst,
                    billingLast,
                    billingEmail,
                    billingCompany
                );
            }

            bool hasFooEventsMetadata = hasLegacyMetadata || hasPluginMetadata;

            // STEP 4: Build DTO
            var dto = BuildOrderBaseDto(
                root,
                raw,
                wooId,
                billingEmail,
                billingFirst,
                billingLast,
                billingCompany,
                hasFooEventsMetadata
            );

            dto.Tickets = baseTickets;
            return dto;
        }

        // ==============================================================
        // BUILD TICKETS (THIS IS THE ONLY CORRECT VERSION)
        // ==============================================================
        private List<CanonicalTicketDto> BuildTicketsFromLineItems(
            JsonElement root,
            string billingFirst,
            string billingLast,
            string billingEmail,
            string billingCompany,
            long? pluginProductId)
        {
            var result = new List<CanonicalTicketDto>();

            if (!root.TryGetProperty("line_items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
            {
                return result;
            }

            foreach (var li in items.EnumerateArray())
            {
                string sku = li.TryGetProperty("sku", out var skuProp)
                    ? skuProp.GetString() ?? ""
                    : "";

                if (string.IsNullOrWhiteSpace(sku))
                {
                    var name = li.TryGetProperty("name", out var nameProp)
                        ? nameProp.GetString()
                        : null;

                    DateTime? orderDate = null;
                    if (root.TryGetProperty("date_created", out var dc) &&
                        DateTime.TryParse(dc.GetString(), out var parsed))
                    {
                        orderDate = parsed;
                    }

                    var startDate = WooCourseParsingHelper.TryParseStartDateFromName(name, orderDate);
                    var code = WooCourseParsingHelper.ExtractCourseCodeFromName(name);

                    if (startDate.HasValue)
                        sku = $"{code}-{startDate.Value:ddMMyy}";
                    else
                        sku = $"{code}-{Guid.NewGuid().ToString("N")[..6]}";

                    _logger.LogWarning(
                        "WooOrderParser. SKU missing. Generated fallback SKU {Sku} for product '{Name}' order {OrderId}",
                        sku,
                        name,
                        root.TryGetProperty("id", out var idProp2) ? idProp2.ToString() : "?"
                    );
                }

                long? productId = null;
                if (li.TryGetProperty("product_id", out var pidProp) &&
                    pidProp.TryGetInt64(out var pidVal))
                {
                    productId = pidVal;
                }

                int quantity = li.TryGetProperty("quantity", out var qProp) &&
                               qProp.TryGetInt32(out var q)
                    ? q
                    : 1;

                for (int i = 0; i < quantity; i++)
                {
                    var ticket = new CanonicalTicketDto
                    {
                        Sku = sku,
                        ProductId = productId,
                        FirstName = billingFirst,
                        LastName = billingLast,
                        Email = billingEmail,
                        Company = billingCompany,
                        RawLineItem = li.Clone(),
                        RawOrderPayload = root.Clone()
                    };

                    if ((!ticket.ProductId.HasValue || ticket.ProductId.Value == 0)
                        && pluginProductId.HasValue
                        && pluginProductId.Value > 0)
                    {
                        ticket.ProductId = pluginProductId.Value;
                    }

                    result.Add(ticket);
                }
            }

            return result;
        }

        // ==============================================================
        // HELPERS
        // ==============================================================
        private void ApplyLegacyTicketData(
            List<CanonicalTicketDto> baseTickets,
            List<WooOrderTicketMapper.TicketDto> legacyTickets,
            string billingFirst,
            string billingLast,
            string billingEmail,
            string billingCompany)
        {
            int count = Math.Min(baseTickets.Count, legacyTickets.Count);

            for (int i = 0; i < count; i++)
            {
                var legacy = legacyTickets[i];
                var ticket = baseTickets[i];

                ticket.FirstName = legacy.FirstName ?? billingFirst;
                ticket.LastName = legacy.LastName ?? billingLast;
                ticket.Email = string.IsNullOrWhiteSpace(legacy.Email)
                    ? billingEmail
                    : legacy.Email;
                ticket.Company = string.IsNullOrWhiteSpace(legacy.Company)
                    ? billingCompany
                    : legacy.Company;

                // THE MISSING PART
                if (legacy.ProductId.HasValue && legacy.ProductId.Value > 0)
                {
                    ticket.ProductId = legacy.ProductId.Value;
                }
            }
        }


        private async Task<bool> TryApplyFooEventsPluginAsync(
            int orderId,
            List<CanonicalTicketDto> baseTickets,
            string billingFirst,
            string billingLast,
            string billingEmail,
            string billingCompany)
        {
            try
            {
                var pluginTickets = await _fooEvents.FetchTicketsForOrderAsync(orderId);

                if (pluginTickets == null || pluginTickets.Count == 0)
                {
                    return false;
                }

                int count = Math.Min(baseTickets.Count, pluginTickets.Count);

                for (int i = 0; i < count; i++)
                {
                    var ft = pluginTickets[i];
                    var ticket = baseTickets[i];

                    string full = ft.AttendeeName ?? string.Empty;

                    ticket.FirstName = SplitFirst(full) ?? billingFirst;
                    ticket.LastName = SplitLast(full) ?? billingLast;

                    ticket.Email = string.IsNullOrWhiteSpace(ft.AttendeeEmail)
                        ? billingEmail
                        : ft.AttendeeEmail;

                    ticket.Company = billingCompany;

                    if ((ticket.ProductId == null || ticket.ProductId == 0)
                        && ft.ProductId > 0)
                    {
                        ticket.ProductId = ft.ProductId;
                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "FooEvents API error for order {OrderId}, continuing with line_items only.",
                    orderId);

                return false;
            }
        }

        private CanonicalWooOrderDto BuildOrderBaseDto(
    JsonElement root,
    RawOrder raw,
    int wooId,
    string billingEmail,
    string billingFirst,
    string billingLast,
    string billingCompany,
    bool hasFooEventsMetadata)
        {
            int totalQty = 0;
            decimal subtotal = 0;
            decimal totalTax = 0;

            if (root.TryGetProperty("line_items", out var lineItems) &&
                lineItems.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in lineItems.EnumerateArray())
                {
                    if (item.TryGetProperty("quantity", out var qProp) &&
                        qProp.TryGetInt32(out var q))
                        totalQty += q;

                    if (item.TryGetProperty("subtotal", out var subProp))
                        subtotal += SafeDecimal(subProp);

                    if (item.TryGetProperty("total_tax", out var taxProp))
                        totalTax += SafeDecimal(taxProp);
                }
            }

            decimal orderTotal = root.TryGetProperty("total", out var totalProp)
                ? SafeDecimal(totalProp)
                : 0m;

            decimal refundTotal = 0m;
            if (root.TryGetProperty("refunds", out var refundsProp) &&
                refundsProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var refund in refundsProp.EnumerateArray())
                {
                    if (refund.TryGetProperty("total", out var rTotal))
                        refundTotal += SafeDecimal(rTotal);
                }
            }

            string currency = root.TryGetProperty("currency", out var curProp)
                ? curProp.GetString() ?? "GBP"
                : "GBP";

            DateTime? dateCreated = TryGetDate(root, "date_created");

            return new CanonicalWooOrderDto
            {
                RawOrderId = raw.Id,
                ExternalId = wooId.ToString(),

                BillingEmail = billingEmail,
                BillingName = $"{billingFirst} {billingLast}".Trim(),
                BillingCompany = billingCompany,

                TotalQuantity = totalQty,
                SubTotal = subtotal,
                TotalTax = totalTax,
                Total = orderTotal,
                PaymentTotal = orderTotal,
                RefundTotal = refundTotal,
                Currency = currency,

                Status = root.TryGetProperty("status", out var statusProp)
                    ? statusProp.GetString() ?? "pending"
                    : "pending",

                HasFooEventsMetadata = hasFooEventsMetadata,
                RawPayload = raw.Payload,

                DateCreated = dateCreated,
                Tickets = new List<CanonicalTicketDto>()
            };
        }
        //
        // private static string ExtractCourseCodeFromName(string name)
        // {
        //     if (string.IsNullOrWhiteSpace(name))
        //         return "COURSE";
        //
        //     var beforeDash = name.Split('-')[0];
        //     var words = beforeDash.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //
        //     var chars = words
        //         .Where(w => char.IsLetter(w[0]) && char.IsUpper(w[0]))
        //         .Select(w => w[0])
        //         .ToArray();
        //
        //     return chars.Length >= 2 ? new string(chars) : "COURSE";
        // }
        //
        // private static DateTime? TryParseStartDateFromName(string name, DateTime? baseDate)
        // {
        //     if (string.IsNullOrWhiteSpace(name))
        //         return null;
        //
        //     var cleaned = name.Replace("™", "").Trim();
        //
        //     const string marker = " - ";
        //     var idx = cleaned.LastIndexOf(marker, StringComparison.Ordinal);
        //     var segment = idx >= 0 ? cleaned[(idx + marker.Length)..].Trim() : cleaned;
        //
        //     var tokens = segment
        //         .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //
        //     if (tokens.Length == 0)
        //         return null;
        //
        //     var dayToken = tokens[0];
        //     if (dayToken.Contains('−') || dayToken.Contains('-'))
        //     {
        //         var parts = dayToken.Split('-', '−');
        //         dayToken = parts[0];
        //     }
        //
        //     if (!int.TryParse(dayToken, out var day) || day <= 0 || day > 31)
        //         return null;
        //
        //     string? monthText = null;
        //     for (var i = 0; i < tokens.Length; i++)
        //     {
        //         var t = tokens[i];
        //         var letters = new string(t.TakeWhile(char.IsLetter).ToArray());
        //         if (!string.IsNullOrEmpty(letters))
        //         {
        //             monthText = letters;
        //             break;
        //         }
        //     }
        //
        //     if (string.IsNullOrEmpty(monthText))
        //         return null;
        //
        //     if (!DateTime.TryParseExact(
        //             monthText,
        //             new[] { "MMM", "MMMM" },
        //             CultureInfo.InvariantCulture,
        //             DateTimeStyles.None,
        //             out var monthDate))
        //     {
        //         return null;
        //     }
        //
        //     var month = monthDate.Month;
        //
        //     int? year = null;
        //     for (var i = tokens.Length - 1; i >= 0; i--)
        //     {
        //         var t = tokens[i];
        //         if (t.All(char.IsDigit) && (t.Length == 2 || t.Length == 4))
        //         {
        //             if (int.TryParse(t, out var y))
        //             {
        //                 if (t.Length == 2)
        //                     y += 2000;
        //
        //                 year = y;
        //                 break;
        //             }
        //         }
        //     }
        //
        //     var reference = baseDate ?? DateTime.UtcNow;
        //
        //     if (!year.HasValue)
        //     {
        //         year = month < reference.Month ? reference.Year + 1 : reference.Year;
        //     }
        //
        //     return new DateTime(year.Value, month, day);
        // }

        private string SafeGet(JsonElement obj, string prop)
        {
            if (obj.ValueKind != JsonValueKind.Object) return string.Empty;
            if (!obj.TryGetProperty(prop, out var val)) return string.Empty;
            return val.ValueKind == JsonValueKind.String ? val.GetString() ?? string.Empty : string.Empty;
        }

        private decimal SafeDecimal(JsonElement el)
        {
            if (el.ValueKind == JsonValueKind.Number)
                return el.GetDecimal();

            if (el.ValueKind == JsonValueKind.String &&
                decimal.TryParse(el.GetString(), out var d))
            {
                return d;
            }

            return 0m;
        }

        private DateTime? TryGetDate(JsonElement root, string prop)
        {
            if (!root.TryGetProperty(prop, out var el))
                return null;

            if (el.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(el.GetString(), out var dt))
            {
                return dt;
            }

            return null;
        }

        private static string SplitFirst(string full)
        {
            if (string.IsNullOrWhiteSpace(full))
                return string.Empty;

            var parts = full.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : string.Empty;
        }

        private static string SplitLast(string full)
        {
            if (string.IsNullOrWhiteSpace(full))
                return string.Empty;

            var parts = full.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 ? parts[1] : string.Empty;
        }

        private long? ExtractFooEventsProductId(JsonElement root)
        {
            if (!root.TryGetProperty("meta_data", out var meta) ||
                meta.ValueKind != JsonValueKind.Array)
                return null;

            foreach (var m in meta.EnumerateArray())
            {
                var key = m.TryGetProperty("key", out var kp) ? kp.GetString() : null;

                // We only care about the WooCommerceEventsOrderTickets metadata block
                if (key != "WooCommerceEventsOrderTickets")
                    continue;

                if (!m.TryGetProperty("value", out var valueElement))
                    continue;

                // valueElement is a nested object: { "1": { "1": { ... } } }
                foreach (var courseEntry in valueElement.EnumerateObject())
                {
                    var attendees = courseEntry.Value;

                    foreach (var attendeeEntry in attendees.EnumerateObject())
                    {
                        var ticket = attendeeEntry.Value;

                        if (ticket.TryGetProperty("WooCommerceEventsProductID", out var pidProp))
                        {
                            var pidString = pidProp.GetString();
                            if (long.TryParse(pidString, out var pid))
                                return pid;
                        }
                    }
                }
            }

            return null;
        }



    }
}
