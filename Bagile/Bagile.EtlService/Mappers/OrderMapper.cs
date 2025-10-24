using System.Text.Json;
using Bagile.Domain.Entities;

namespace Bagile.EtlService.Mappers
{
    public static class OrderMapper
    {
        public static Order? MapFromRaw(string source, long rawOrderId, string payload)
        {
            var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var order = new Order { RawOrderId = rawOrderId, Source = source };

            switch (source.ToLower())
            {
                case "woo":
                    order.Type = "public";

                    order.ExternalId = root.TryGetProperty("id", out var idProp)
                        ? idProp.GetRawText()
                        : string.Empty;

                    order.TotalAmount = root.TryGetProperty("total", out var totalProp)
                        ? decimal.Parse(totalProp.GetString() ?? "0")
                        : 0;

                    order.Status = root.TryGetProperty("status", out var s) ? s.GetString() : null;

                    if (root.TryGetProperty("billing", out var billing))
                    {
                        order.BillingCompany = billing.TryGetProperty("company", out var comp) ? comp.GetString() : null;

                        var first = billing.TryGetProperty("first_name", out var fn) ? fn.GetString() : "";
                        var last = billing.TryGetProperty("last_name", out var ln) ? ln.GetString() : "";
                        order.ContactName = $"{first} {last}".Trim();

                        order.ContactEmail = billing.TryGetProperty("email", out var em) ? em.GetString() : null;
                    }

                    order.Reference = root.TryGetProperty("number", out var num)
                        ? num.GetString()
                        : root.TryGetProperty("id", out var idRef)
                            ? idRef.GetRawText()
                            : string.Empty;

                    order.OrderDate = root.TryGetProperty("date_created", out var dt)
                        ? DateTime.Parse(dt.GetString() ?? DateTime.UtcNow.ToString())
                        : DateTime.UtcNow;
                    break;

                case "xero":
                    order.Type = "private";
                    order.ExternalId = root.TryGetProperty("InvoiceID", out var inv)
                        ? inv.GetString() ?? ""
                        : "";
                    order.TotalAmount = root.TryGetProperty("Total", out var t)
                        ? decimal.Parse(t.GetRawText())
                        : 0;
                    order.Status = root.TryGetProperty("Status", out var stat) ? stat.GetString() : null;

                    if (root.TryGetProperty("Reference", out var refProp))
                        order.Reference = refProp.GetString();

                    if (root.TryGetProperty("Contact", out var contact))
                    {
                        order.BillingCompany = contact.TryGetProperty("Name", out var nameProp) ? nameProp.GetString() : null;
                        order.ContactEmail = contact.TryGetProperty("EmailAddress", out var emailProp) ? emailProp.GetString() : null;
                    }

                    order.OrderDate = root.TryGetProperty("DateString", out var ds)
                        ? DateTime.Parse(ds.GetString() ?? DateTime.UtcNow.ToString())
                        : DateTime.UtcNow;
                    break;

                default:
                    return null;
            }

            return order;
        }
    }
}
