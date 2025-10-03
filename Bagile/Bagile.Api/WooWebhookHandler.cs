using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bagile.Api;

public class WooWebhookSourceHandler : IWebhookSourceHandler
{
    public string Source => "woo";

    public bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger)
    {
        var secret = config.GetValue<string>("WooCommerce:WebhookSecret");
        if (string.IsNullOrEmpty(secret))
            return true; // no secret configured, skip check

        if (!http.Request.Headers.TryGetValue("X-WC-Webhook-Signature", out var sigHeaders) || string.IsNullOrEmpty(sigHeaders))
        {
            logger.LogWarning("Missing WooCommerce webhook signature header");
            return false;
        }

        var providedSig = sigHeaders.ToString();
        try
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var computed = hmac.ComputeHash(bodyBytes);
            var computedBase64 = Convert.ToBase64String(computed);

            var prov = Convert.FromBase64String(providedSig);
            var comp = Convert.FromBase64String(computedBase64);

            return prov.Length == comp.Length && CryptographicOperations.FixedTimeEquals(prov, comp);
        }
        catch
        {
            return false;
        }
    }

    public bool TryExtractExternalId(string body, out string externalId, ILogger logger)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            externalId = doc.RootElement.GetProperty("id").GetRawText().Trim('"');
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid WooCommerce payload: missing or malformed 'id'");
            externalId = "";
            return false;
        }
    }

    public string? ExtractEventType(HttpContext http, string body)
    {
        return http.Request.Headers["X-WC-Webhook-Topic"].FirstOrDefault();
    }
}
