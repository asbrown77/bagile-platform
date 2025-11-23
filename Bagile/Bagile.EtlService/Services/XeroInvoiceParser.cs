using Bagile.Domain.Entities;
using Bagile.EtlService.Models;
using System.Text.Json;

namespace Bagile.EtlService.Services
{
    public class XeroInvoiceParser : IParser<CanonicalXeroInvoiceDto>
    {
        public Task<CanonicalXeroInvoiceDto> Parse(RawOrder raw)
        {
            using var doc = JsonDocument.Parse(raw.Payload);
            var root = doc.RootElement;

            string invoiceId =
                root.TryGetProperty("InvoiceID", out var idProp)
                    ? idProp.GetString() ?? string.Empty : String.Empty;

            string externalId =
                root.TryGetProperty("InvoiceNumber", out var invNumProp)
                    ? invNumProp.GetString() ?? raw.ExternalId ?? ""
                    : raw.ExternalId ?? "";

            string status =
                root.TryGetProperty("Status", out var stProp)
                    ? stProp.GetString() ?? "unknown"
                    : "unknown";

            string email = "";
            string name = "";
            string company = "";

            if (root.TryGetProperty("Contact", out var contact))
            {
                email = contact.TryGetProperty("EmailAddress", out var e) ? e.GetString() ?? "" : "";

                var first = contact.TryGetProperty("FirstName", out var fn) ? fn.GetString() ?? "" : "";
                var last = contact.TryGetProperty("LastName", out var ln) ? ln.GetString() ?? "" : "";

                name = $"{first} {last}".Trim();

                company = contact.TryGetProperty("Name", out var cn) ? cn.GetString() ?? "" : "";
            }

            decimal safeDec(JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Number) return el.GetDecimal();
                if (el.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(el.GetString(), out var d)) return d;
                return 0;
            }

             return Task.FromResult(new CanonicalXeroInvoiceDto
            {
                RawOrderId = raw.Id,
                InvoiceId = invoiceId,
                ExternalId = externalId,
                Status = status,

                BillingEmail = email,
                BillingName = name,
                BillingCompany = company,

                SubTotal = root.TryGetProperty("SubTotal", out var sub) ? safeDec(sub) : 0,
                TotalTax = root.TryGetProperty("TotalTax", out var tax) ? safeDec(tax) : 0,
                Total = root.TryGetProperty("Total", out var total) ? safeDec(total) : 0,
                AmountPaid = root.TryGetProperty("AmountPaid", out var paid) ? safeDec(paid) : 0,
                AmountDue = root.TryGetProperty("AmountDue", out var due) ? safeDec(due) : 0,
                Reference = root.TryGetProperty("Reference", out var reference) ? reference.GetString() ?? "" : "",
                Currency = root.TryGetProperty("CurrencyCode", out var cur)
                    ? cur.GetString() ?? "GBP"
                    : "GBP",

                InvoiceDate = root.TryGetProperty("Date", out var dateProp)
                    && dateProp.ValueKind == JsonValueKind.String
                    && DateTime.TryParse(dateProp.GetString(), out var d)
                        ? d : null,

                RawPayload = raw.Payload
            });
        }
    }

}