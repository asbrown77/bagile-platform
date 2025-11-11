using Bagile.EtlService.Projectors;
using Bagile.Infrastructure;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bagile.EtlService.Collectors;

public class WooOrderCollector : ISourceCollector
{
    private readonly IWooApiClient _woo;
    private readonly ILogger<WooOrderCollector> _logger;

    public string SourceName => "woo";


    public WooOrderCollector(
        IWooApiClient woo,
        ILogger<WooOrderCollector> logger)
    {
        _woo = woo;
        _logger = logger;
    }

    // ✅ Public single entry point
    public async Task<IEnumerable<string>> CollectOrdersAsync(DateTime? modifiedSince = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Collecting WooCommerce orders...");

        var allOrders = new List<string>();
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            ct.ThrowIfCancellationRequested(); 

            _logger.LogInformation("Fetching page {Page}", page);
            var orders = await _woo.FetchOrdersAsync(page, pageSize, modifiedSince, ct);

            if (orders == null || orders.Count == 0)
            {
                _logger.LogInformation("No more orders after page {Page}", page);
                break;
            }

            allOrders.AddRange(orders);

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
