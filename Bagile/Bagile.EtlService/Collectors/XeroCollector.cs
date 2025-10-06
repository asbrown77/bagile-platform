using System.Text.Json;
using Bagile.Infrastructure;
using Bagile.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Collectors;

public class XeroCollector : ISourceCollector
{
    private readonly IXeroApiClient _xero;
    private readonly ILogger<XeroCollector> _logger;

    public string SourceName => "xero";

    public XeroCollector(IXeroApiClient xero, ILogger<XeroCollector> logger)
    {
        _xero = xero;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> CollectAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Collecting Xero invoices...");
        var invoices = await _xero.FetchInvoicesAsync(ct);

        var filtered = new List<string>();

        foreach (var json in invoices)
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("Status", out var status) &&
                (status.GetString() == "AUTHORISED" || status.GetString() == "PAID"))
            {
                filtered.Add(doc.RootElement.GetRawText());
            }
        }

        _logger.LogInformation("XeroCollector kept {Count} invoices", filtered.Count);
        return filtered;
    }
}