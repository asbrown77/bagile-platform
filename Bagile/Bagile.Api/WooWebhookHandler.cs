using System.Text;
using System.Text.Json;

namespace Bagile.Api;

public class WooWebhookHandler : IWebhookHandler
{
    public string Source => "woo";

    public bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger)
    {
        var secret = config.GetValue<string>("WooCommerce:WebhookSecret");
        if (string.IsNullOrEmpty(secret))
        {
            logger.LogWarning("Woo secret not configured");
            return false;
        }

        if (!http.Request.Headers.TryGetValue("X-WC-Webhook-Signature", out var header))
        {
            logger.LogWarning("Missing X-WC-Webhook-Signature header");
            return false;
        }

        using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = Convert.ToBase64String(hmac.ComputeHash(bodyBytes));

        var valid = string.Equals(header, computed, StringComparison.Ordinal);
        logger.LogInformation("Woo webhook signature validation result: {Valid}", valid);

        return valid;
    }

    public bool TryPreparePayload(string body, HttpContext http, IConfiguration config, ILogger logger,
        out string externalId, out string payloadJson, out string eventType)
    {
        logger.LogInformation("Received Woo webhook payload: {Payload}", body);

        externalId = string.Empty;
        payloadJson = string.Empty;
        eventType = string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(body);
            externalId = doc.RootElement.GetProperty("id").GetInt32().ToString();
            payloadJson = body;

            eventType = http.Request.Headers.TryGetValue("X-WC-Webhook-Event", out var evt)
                ? evt.ToString()
                : "order.unknown";

            logger.LogInformation("Woo webhook parsed: ExternalId={ExternalId}, EventType={EventType}",
                externalId, eventType);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid Woo payload");
            return false;
        }
    }
}
