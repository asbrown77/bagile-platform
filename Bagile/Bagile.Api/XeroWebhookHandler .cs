using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bagile.Api;

public class XeroWebhookandler : IWebhookHandler
{
    public string Source => "xero";

    public bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger)
    {
        var secret = config.GetValue<string>("Xero:WebhookSecret");
        if (string.IsNullOrEmpty(secret)) return false;

        if (!http.Request.Headers.TryGetValue("x-xero-signature", out var header)) return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedBase64 = Convert.ToBase64String(hmac.ComputeHash(bodyBytes));

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(header!),
            Convert.FromBase64String(computedBase64)
        );
    }

    public bool TryPreparePayload(string body, HttpContext http, IConfiguration config, ILogger logger,
        out string externalId, out string payloadJson, out string eventType)
    {
        externalId = string.Empty;
        payloadJson = string.Empty;
        eventType = "INVOICE";

        try
        {
            using var doc = JsonDocument.Parse(body);
            externalId = doc.RootElement.GetProperty("events")[0].GetProperty("resourceId").GetString() ?? "";
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid Xero payload");
            return false;
        }

        var xeroClient = new XeroApiClient(config, logger);
        var invoice = xeroClient.GetInvoiceByIdAsync(externalId).GetAwaiter().GetResult();

        if (XeroInvoiceFilter.ShouldCapture(invoice))
        {
            payloadJson = JsonSerializer.Serialize(invoice);
            return true;
        }

        logger.LogInformation("Ignored Xero invoice {Id} (Type={Type}, Status={Status}, Ref={Ref})",
            invoice?.InvoiceID, invoice?.Type, invoice?.Status, invoice?.Reference);

        return false;
    }
}
