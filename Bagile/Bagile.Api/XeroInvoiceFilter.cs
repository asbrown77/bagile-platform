using Bagile.Infrastructure.Models;

namespace Bagile.Api;

public static class XeroInvoiceFilter
{
    public static bool ShouldCapture(XeroInvoice invoice)
    {
        return invoice.Type == "ACCREC"
               && (invoice.Status == "AUTHORISED"
                   || invoice.Status == "PAID"
                   || invoice.Status == "VOIDED")
               && !(invoice.Reference?.StartsWith("#") ?? false);
    }
}