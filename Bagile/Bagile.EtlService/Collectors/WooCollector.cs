using System.Text.Json;
using Bagile.Infrastructure;
using Bagile.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Collectors;

public class WooCollector : ISourceCollector
{
    private readonly IWooApiClient _woo;
    private readonly ILogger<WooCollector> _logger;

    public string SourceName => "woo";

    public WooCollector(IWooApiClient woo, ILogger<WooCollector> logger)
    {
        _woo = woo;
        _logger = logger;
    }

    public async Task<IEnumerable<string>> CollectAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Collecting WooCommerce orders...");
        var since = DateTime.UtcNow.AddDays(-2);

        var orders = await _woo.FetchOrdersAsync(since, ct);
        _logger.LogInformation("WooCollector got {Count} orders", orders.Count);

        return orders;
    }
}