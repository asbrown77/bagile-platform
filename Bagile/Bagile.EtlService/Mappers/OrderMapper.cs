using System.Data.Common;
using System.Text.Json;
using Bagile.Domain.Entities;

namespace Bagile.EtlService.Mappers
{
    public static class OrderMapper
    {
        public static Order? MapFromRaw(RawOrder rawOrder)
        {
            var doc = JsonDocument.Parse(rawOrder.Payload);
            var root = doc.RootElement;

            var order = new Order { RawOrderId = rawOrder.Id, Source = rawOrder.Source };

            switch (order.Source.ToLower())
            {
                case "woo":
                    // Only process real Woo orders with id and line_items
                    if (root.ValueKind != JsonValueKind.Object ||
                        !root.TryGetProperty("id", out _) ||
                        !root.TryGetProperty("line_items", out var lineItems) ||
                        lineItems.ValueKind != JsonValueKind.Array)
                    {
                        return null;
                    }

                    order.Type = "public";

                    order.ExternalId = root.TryGetProperty("id", out var idProp)
                        ? idProp.GetRawText()
                        : string.Empty;

                    order.TotalAmount = root.TryGetProperty("total", out var totalProp)
                        ? decimal.Parse(totalProp.GetString() ?? "0")
                        : 0;

                    order.TotalTax = root.TryGetProperty("total_tax", out var taxProp)
                        ? decimal.Parse(taxProp.GetString() ?? "0")
                        : 0;

                    order.SubTotal = order.TotalAmount - order.TotalTax;


                    if (root.TryGetProperty("line_items", out var items))
                    {
                        order.TotalQuantity = items
                            .EnumerateArray()
                            .Sum(i => i.TryGetProperty("quantity", out var q) ? q.GetInt32() : 0);
                    }

                    order.Status = root.TryGetProperty("status", out var s)
                        ? s.GetString()
                        : null;

                    if (root.TryGetProperty("billing", out var billing))
                    {
                        order.BillingCompany = billing.TryGetProperty("company", out var comp)
                            ? comp.GetString()
                            : null;

                        var first = billing.TryGetProperty("first_name", out var fn) ? fn.GetString() : "";
                        var last = billing.TryGetProperty("last_name", out var ln) ? ln.GetString() : "";
                        order.ContactName = $"{first} {last}".Trim();

                        order.ContactEmail = billing.TryGetProperty("email", out var em)
                            ? em.GetString()
                            : null;
                    }

                    order.Reference = root.TryGetProperty("number", out var num)
                        ? num.GetString()
                        : root.TryGetProperty("id", out var idRef)
                            ? idRef.GetRawText()
                            : string.Empty;

                    order.OrderDate = root.TryGetProperty("date_created", out var dt)
                        ? DateTime.Parse(dt.GetString() ?? DateTime.UtcNow.ToString())
                        : DateTime.UtcNow;

                    // Normalize before return
                    order.Status = NormalizeStatus(order.Source, order.Status);
                    break;

                case "xero":
                    // Only process real ACCREC invoices
                    if (!root.TryGetProperty("Type", out var typeProp) ||
                        !string.Equals(typeProp.GetString(), "ACCREC", StringComparison.OrdinalIgnoreCase) ||
                        !root.TryGetProperty("InvoiceID", out _))
                    {
                        return null;
                    }

                    order.Type = "private";

                    order.ExternalId = root.TryGetProperty("InvoiceNumber", out var invNum)
                        ? invNum.GetString() ?? ""
                        : "";

                    order.TotalAmount = root.TryGetProperty("Total", out var total)
                        ? total.GetDecimal()
                        : 0;

                    order.TotalTax = root.TryGetProperty("TotalTax", out var tax)
                        ? tax.GetDecimal()
                        : 0;

                    order.SubTotal = root.TryGetProperty("SubTotal", out var sub)
                        ? sub.GetDecimal()
                        : order.TotalAmount - order.TotalTax;

                    order.Status = root.TryGetProperty("Status", out var stat)
                        ? stat.GetString()
                        : null;

                    if (root.TryGetProperty("Reference", out var refProp))
                        order.Reference = refProp.GetString();

                    if (root.TryGetProperty("Contact", out var contact))
                    {
                        order.BillingCompany = contact.TryGetProperty("Name", out var nameProp)
                            ? nameProp.GetString()
                            : null;
                        order.ContactEmail = contact.TryGetProperty("EmailAddress", out var emailProp)
                            ? emailProp.GetString()
                            : null;
                    }

                    order.OrderDate = root.TryGetProperty("DateString", out var ds)
                        ? DateTime.Parse(ds.GetString() ?? DateTime.UtcNow.ToString())
                        : DateTime.UtcNow;

                    // Normalize before return
                    order.Status = NormalizeStatus(order.Source, order.Status);
                    break;

                default:
                    return null;
            }

            return order;
        }

        private static string NormalizeStatus(string? source, string? rawStatus)
        {
            if (string.IsNullOrWhiteSpace(rawStatus))
                return "pending";

            rawStatus = rawStatus.Trim().ToLowerInvariant();
            source = source?.ToLowerInvariant() ?? "";

            return (source, rawStatus) switch
            {
                // WooCommerce
                ("woo", "completed") or ("woo", "processing") => "completed",
                ("woo", "pending") or ("woo", "on-hold") => "pending",
                ("woo", "cancelled") or ("woo", "trash") or ("woo", "refunded") => "cancelled",
                ("woo", "failed") => "failed",

                // Xero
                ("xero", "paid") => "completed",
                ("xero", "authorised") or ("xero", "submitted") or ("xero", "draft") => "pending",
                ("xero", "voided") or ("xero", "deleted") => "cancelled",

                _ => rawStatus
            };
        }
    }
}
