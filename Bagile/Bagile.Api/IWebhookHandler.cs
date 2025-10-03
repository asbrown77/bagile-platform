namespace Bagile.Api;

public interface IWebhookHandler
{
    string Source { get; } // "woo", "xero", etc.
    bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger);
    bool TryPreparePayload(string body, HttpContext http, IConfiguration config, ILogger logger,
        out string externalId, out string payloadJson);
}