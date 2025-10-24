using System.Text.Json;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Collectors;
using Bagile.EtlService.Utils;
using Bagile.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Services;

public class EtlRunner
{
    private readonly IEnumerable<ISourceCollector> _orderCollectors;
    private readonly IEnumerable<IProductCollector> _productCollectors;
    private readonly IRawOrderRepository _rawOrderRepository;
    private readonly ILogger<EtlRunner> _logger;

    public EtlRunner(
        IEnumerable<ISourceCollector> orderCollectors,
        IEnumerable<IProductCollector> productCollectors,
        IRawOrderRepository rawOrderRepository,
        ILogger<EtlRunner> logger)
    {
        _orderCollectors = orderCollectors;
        _productCollectors = productCollectors;
        _rawOrderRepository = rawOrderRepository;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting ETL run");

        await CollectProductsAsync(ct);
        await CollectOrdersAsync(ct);

        _logger.LogInformation("ETL run completed successfully");
    }

    private async Task CollectOrdersAsync(CancellationToken ct)
    {
        foreach (var collector in _orderCollectors)
        {
            var source = collector.SourceName;
            _logger.LogInformation("Collecting orders from {Source}", source);

            var modifiedSince = await _rawOrderRepository.GetLastTimestampAsync(source);
            var payloads = await collector.CollectOrdersAsync(modifiedSince, ct);

            foreach (var raw in payloads)
            {
                var id = JsonHelpers.ExtractId(raw);
                await _rawOrderRepository.InsertIfChangedAsync(source, id, raw, "etl.import");
            }

            _logger.LogInformation("Finished collecting orders from {Source}", source);
        }
    }

    private async Task CollectProductsAsync(CancellationToken ct)
    {
        foreach (var collector in _productCollectors)
        {
            _logger.LogInformation("Collecting products from {Source}", collector.SourceName);
            await collector.CollectProductsAsync(ct);
        }
    }
}
