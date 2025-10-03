using Bagile.Infrastructure;
using System.Text;

namespace Bagile.Api;

public class WebhookHandler
{
    private readonly IEnumerable<IWebhookSourceHandler> _handlers;
    private readonly IConfiguration _config;
    private readonly IRawOrderRepository _repo;
    private readonly ILogger _logger;

    public WebhookHandler(IEnumerable<IWebhookSourceHandler> handlers, IConfiguration config, IRawOrderRepository repo, ILogger<WebhookHandler> logger)
    {
        _handlers = handlers;
        _config = config;
        _repo = repo;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(HttpContext http, string source)
    {
        var handler = _handlers.FirstOrDefault(h => h.Source.Equals(source, StringComparison.OrdinalIgnoreCase));
        if (handler == null)
            return Results.BadRequest($"Unsupported webhook source: {source}");

        var bodyBytes = await ReadRequestBodyAsync(http);

        var maxBytes = _config.GetValue<int?>("Webhook:MaxBodySizeBytes") ?? 1 * 1024 * 1024;
        if (bodyBytes.Length > maxBytes)
            return Results.Problem("Payload too large", statusCode: StatusCodes.Status413PayloadTooLarge);

        if (!handler.IsValidSignature(http, bodyBytes, _config, _logger))
            return Results.Unauthorized();

        var body = Encoding.UTF8.GetString(bodyBytes);

        // Special case: Xero handshake has events: []
        if (source.Equals("xero", StringComparison.OrdinalIgnoreCase) && body.Contains("\"events\": []"))
        {
            _logger.LogInformation("Xero handshake validated successfully");
            return Results.Ok(); // 200 empty body
        }

        // Normal event flow
        if (!handler.TryExtractExternalId(body, out var externalId, _logger))
            return Results.BadRequest("Invalid payload");

        var eventType = handler.ExtractEventType(http, body);

        try
        {
            var id = await _repo.InsertAsync(source, externalId, body, eventType);
            _logger.LogInformation("Stored raw event source={Source}, externalId={ExternalId}, eventType={EventType}, id={Id}",
                source, externalId, eventType, id);

            return Results.Ok(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert raw event source={Source}, externalId={ExternalId}", source, externalId);
            return Results.Problem("Failed to persist payload", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
    private async Task<byte[]> ReadRequestBodyAsync(HttpContext http)
    {
        using var ms = new MemoryStream();
        await http.Request.Body.CopyToAsync(ms);
        return ms.ToArray();
    }
}
