namespace Bagile.Api;

public interface IWebhookSourceHandler
{
    string Source { get; } // "woo", "xero", etc.

    bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger);
    bool TryExtractExternalId(string body, out string externalId, ILogger logger);
    string? ExtractEventType(HttpContext http, string body);
}