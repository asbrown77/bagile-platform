using System.Text;
using Bagile.Infrastructure.Repositories;

namespace Bagile.Api.Handlers;

public class WebhookHandler
{
    private readonly IEnumerable<IWebhookHandler> _handlers;
    private readonly IConfiguration _config;
    private readonly IRawOrderRepository _repo;
    private readonly ILogger<WebhookHandler> _logger;

    public WebhookHandler(
        IEnumerable<IWebhookHandler> handlers,
        IConfiguration config,
        IRawOrderRepository repo,
        ILogger<WebhookHandler> logger)
    {
        _handlers = handlers;
        _config = config;
        _repo = repo;
        _logger = logger;
    }

    public async Task<IResult> HandleAsync(HttpContext http, string source)
    {
        var handler = _handlers.FirstOrDefault(h =>
            h.Source.Equals(source, StringComparison.OrdinalIgnoreCase));

        if (handler == null)
        {
            _logger.LogWarning("Unsupported webhook source: {Source}", source);
            return Results.BadRequest($"Unsupported webhook source: {source}");
        }

        var bodyBytes = await ReadRequestBodyAsync(http);

        if (!handler.IsValidSignature(http, bodyBytes, _config, _logger))
        {
            _logger.LogWarning("Invalid signature for webhook source: {Source}", source);
            return Results.Unauthorized();
        }

        var body = Encoding.UTF8.GetString(bodyBytes);

        var payload = handler.PreparePayload(body, http, _config, _logger);
        if (payload == null)
        {
            _logger.LogInformation("Ignored or invalid payload for source: {Source}", source);
            return Results.Ok(); // still return 200 to stop retries
        }

        if (!string.IsNullOrEmpty(payload.ExternalId))
        {
            _logger.LogInformation(
                "Inserting webhook event: Source={Source}, ExternalId={ExternalId}, EventType={EventType}",
                payload.Source, payload.ExternalId, payload.EventType);

            await _repo.InsertAsync(payload.Source, payload.ExternalId, payload.PayloadJson, payload.EventType);
        }

        return Results.Ok();
    }

    private static async Task<byte[]> ReadRequestBodyAsync(HttpContext http)
    {
        http.Request.EnableBuffering(); // allow multiple reads

        using var ms = new MemoryStream();
        await http.Request.Body.CopyToAsync(ms);
        var bodyBytes = ms.ToArray();

        // rewind so the rest of the pipeline or model binders can still read it
        http.Request.Body.Position = 0;

        return bodyBytes;
    }

}
