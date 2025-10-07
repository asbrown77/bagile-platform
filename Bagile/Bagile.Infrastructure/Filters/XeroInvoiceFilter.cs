using Bagile.Infrastructure.Models;

namespace Bagile.Infrastructure.Filters;

public static class XeroInvoiceFilter
{
    public static bool ShouldCapture(XeroInvoice invoice)
    {
        return IsInvoiceType(invoice)
               && IsAuthorised(invoice)
               && IsNotPublicOrder(invoice);
    }

    private static bool IsInvoiceType(XeroInvoice invoice) =>
        invoice.Type == "ACCREC";

    private static bool IsAuthorised(XeroInvoice invoice) =>
        invoice.Status == "AUTHORISED"
        || invoice.Status == "PAID"
        || invoice.Status == "VOIDED";

    // In Xero, public / WooCommerce invoices use a "#" reference prefix.
    private static bool IsNotPublicOrder(XeroInvoice invoice) =>
        !(invoice.Reference?.StartsWith("#") ?? false);

    public static string ToXeroWhereClause(DateTime? modifiedSince = null)
    {
        var whereParts = new List<string>
        {
            "Type==\"ACCREC\"",
            "(Status==\"AUTHORISED\"||Status==\"PAID\"||Status==\"VOIDED\")"
        };

        if (modifiedSince.HasValue && modifiedSince.Value > DateTime.MinValue)
        {
            var date = modifiedSince.Value.ToString("yyyy-MM-dd");
            whereParts.Add($"Date>=DateTime({date})");
        }

        return string.Join("&&", whereParts);
    }

}