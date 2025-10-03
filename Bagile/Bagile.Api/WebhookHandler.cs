using Bagile.Infrastructure;
using System.Text;

namespace Bagile.Api;

public class WebhookHandler
{
    private readonly IEnumerable<IWebhookHandler> _handlers;
    private readonly IConfiguration _config;
    private readonly IRawOrderRepository _repo;
    private readonly ILogger _logger;

    public WebhookHandler(IEnumerable<IWebhookHandler> handlers, IConfiguration config,
        IRawOrderRepository repo, ILogger<WebhookHandler> logger)
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
            return Results.BadRequest($"Unsupported webhook source: {source}");

        var bodyBytes = await ReadRequestBodyAsync(http);

        if (!handler.IsValidSignature(http, bodyBytes, _config, _logger))
            return Results.Unauthorized();

        var body = Encoding.UTF8.GetString(bodyBytes);

        if (handler.TryPreparePayload(body, http, _config, _logger,
                out var externalId, out var payloadJson, out var eventType))
        {
            await _repo.InsertAsync(source, externalId, payloadJson, eventType);
            return Results.Ok();
        }

        return Results.BadRequest("Invalid or ignored webhook payload");
    }

    private static async Task<byte[]> ReadRequestBodyAsync(HttpContext http)
    {
        using var ms = new MemoryStream();
        await http.Request.Body.CopyToAsync(ms);
        return ms.ToArray();
    }
}