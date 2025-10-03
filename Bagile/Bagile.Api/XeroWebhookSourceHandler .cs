using System.Text.Json;

namespace Bagile.Api;

public class XeroWebhookSourceHandler : IWebhookSourceHandler
{
    public string Source => "xero";

    public bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger)
    {
        // TODO: implement Xero signature validation (if using signing key)
        return true;
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