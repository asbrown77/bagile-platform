using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bagile.Api;

public class XeroWebhookHandler : IWebhookHandler
{
    private readonly IXeroApiClient _xeroClient;

    public XeroWebhookHandler(IXeroApiClient xeroClient)
    {
        _xeroClient = xeroClient;
    }

    public string Source => "xero";

    public bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger)
    {
        var secret = config.GetValue<string>("Xero:WebhookSecret");
        if (string.IsNullOrEmpty(secret))
        {
            logger.LogWarning("No Xero webhook secret configured");
            return false;
        }

        if (!http.Request.Headers.TryGetValue("x-xero-signature", out var header))
        {
            logger.LogWarning("Missing x-xero-signature header");
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedBase64 = Convert.ToBase64String(hmac.ComputeHash(bodyBytes));

        var valid = string.Equals(header, computedBase64, StringComparison.Ordinal);
        logger.LogInformation("Xero webhook signature validation result: {Valid}", valid);

        return valid;
    }

    public bool TryPreparePayload(
     string body,
     HttpContext http,
     IConfiguration config,
     ILogger logger,
     out string externalId,
     out string payloadJson,
     out string eventType)
    {
        logger.LogInformation("Received Xero webhook payload: {Payload}", body);

        externalId = string.Empty;
        payloadJson = string.Empty;
        eventType = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(body);
            var events = doc.RootElement.GetProperty("events");

            // Handshake payload → no events, but still valid
            if (events.ValueKind == JsonValueKind.Array && events.GetArrayLength() == 0)
            {
                logger.LogInformation("Xero webhook handshake received (no events). Acknowledging.");
                // Important: return true so caller replies 200 OK
                return true;
            }

            if (events.GetArrayLength() == 0)
            {
                logger.LogInformation("Xero webhook events array empty, ignoring.");
                return true; // also treat as acknowledged
            }

            // Normal event
            var evt = events[0];
            externalId = evt.GetProperty("resourceId").GetString() ?? "";
            var category = evt.GetProperty("eventCategory").GetString() ?? "UNKNOWN";
            var type = evt.GetProperty("eventType").GetString() ?? "UNKNOWN";
            eventType = $"{category}.{type}".ToLower();

            logger.LogInformation("Parsed Xero webhook: ExternalId={ExternalId}, EventType={EventType}",
                externalId, eventType);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid Xero payload");
            return false;
        }

        // If it was just a handshake, externalId will be empty → no DB insert
        if (string.IsNullOrEmpty(externalId))
            return true;

        // Fetch invoice for real events
        var invoice = _xeroClient.GetInvoiceByIdAsync(externalId).GetAwaiter().GetResult();

        if (invoice != null && XeroInvoiceFilter.ShouldCapture(invoice))
        {
            payloadJson = JsonSerializer.Serialize(invoice);
            logger.LogInformation("Captured Xero invoice {Id}", invoice.InvoiceID);
            return true;
        }

        logger.LogInformation(
            "Ignored Xero invoice {Id} (Type={Type}, Status={Status}, Ref={Ref})",
            invoice?.InvoiceID, invoice?.Type, invoice?.Status, invoice?.Reference);

        return true; // still return true so API answers 200 OK
    }



}
