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

    public Task<IEnumerable<string>> CollectAsync(CancellationToken ct = default)
        => CollectAsync(null, ct);

    public async Task<IEnumerable<string>> CollectAsync(DateTime? modifiedSince = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Collecting WooCommerce orders...");

        var allOrders = new List<string>();
        var page = 1;
        const int pageSize = 100; // WooCommerce max per_page = 100

        while (true)
        {
            _logger.LogInformation("Fetching page {Page}", page);

            // assuming your IWooApiClient has a paged overload
            var orders = await _woo.FetchOrdersAsync(page, pageSize, modifiedSince, ct);

            if (orders == null || orders.Count == 0)
            {
                _logger.LogInformation("No more orders after page {Page}", page);
                break;
            }

            allOrders.AddRange(orders);
            _logger.LogInformation("Collected {Count} orders so far", allOrders.Count);

            if (orders.Count < pageSize)
            {
                _logger.LogInformation("Reached last page ({Page}), stopping", page);
                break;
            }

            page++;
        }

        _logger.LogInformation("WooCollector total orders collected: {Total}", allOrders.Count);
        return allOrders;
    }
}