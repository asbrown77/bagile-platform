using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bagile.Api;

public class XeroWebhookSourceHandler : IWebhookSourceHandler
{
    public string Source => "xero";

    public bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger)
    {
        var secret = config.GetValue<string>("Xero:WebhookSecret");
        if (string.IsNullOrEmpty(secret))
            return true; // skip if no secret configured

        if (!http.Request.Headers.TryGetValue("x-xero-signature", out var header))
            return false;

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computed = hmac.ComputeHash(bodyBytes);
        var computedBase64 = Convert.ToBase64String(computed);

        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(header),
            Convert.FromBase64String(computedBase64)
        );
    }

    public bool TryExtractExternalId(string body, out string externalId, ILogger logger)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            externalId = doc.RootElement.GetProperty("events")[0].GetProperty("resourceId").GetString() ?? "";
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Invalid Xero payload: missing resourceId");
            externalId = "";
            return false;
        }
    }

    public string? ExtractEventType(HttpContext http, string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("events")[0].GetProperty("eventCategory").GetString();
        }
        catch
        {
            return null;
        }
    }
}