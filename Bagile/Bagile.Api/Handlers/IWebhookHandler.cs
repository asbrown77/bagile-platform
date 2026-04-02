using Bagile.Api.DTO;

namespace Bagile.Api.Handlers;

public interface IWebhookHandler
{
    string Source { get; }
    bool IsValidSignature(HttpContext http, byte[] bodyBytes, IConfiguration config, ILogger logger);
    Task<WebhookPayload?> PreparePayloadAsync(string body, HttpContext http, IConfiguration config, ILogger logger);
}