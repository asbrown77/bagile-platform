using Bagile.Infrastructure;
using System.Text;
using System.Text.Json;

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

        if (!handler.IsValidSignature(http, bodyBytes, _config, _logger))
            return Results.Unauthorized();

        var body = Encoding.UTF8.GetString(bodyBytes);

        if (source.Equals("xero", StringComparison.OrdinalIgnoreCase))
        {
            if (handler.TryExtractExternalId(body, out var externalId, _logger))
            {
                var xeroClient = new XeroApiClient(_config, _logger); // your API client wrapper
                var invoice = await xeroClient.GetInvoiceByIdAsync(externalId);

                if (XeroInvoiceFilter.ShouldCapture(invoice))
                {
                    await _repo.InsertAsync("xero", externalId, JsonSerializer.Serialize(invoice));
                    return Results.Ok();
                }

                _logger.LogInformation("Ignored Xero invoice {Id} (Type={Type}, Status={Status}, Ref={Ref})",
                    invoice.InvoiceID, invoice.Type, invoice.Status, invoice.Reference);

                return Results.Ok();
            }
        }

        return Results.BadRequest("Invalid webhook payload");
    }

    private async Task<byte[]> ReadRequestBodyAsync(HttpContext http)
    {
        using var ms = new MemoryStream();
        await http.Request.Body.CopyToAsync(ms);
        return ms.ToArray();
    }
}
