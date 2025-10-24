using Bagile.Domain.Repositories;
using Bagile.EtlService.Mappers;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Services;

public class RawOrderProcessor
{
    private readonly IOrderRepository _orderRepo;
    private readonly IRawOrderRepository _rawRepo;
    private readonly ILogger<RawOrderProcessor> _logger;

    public RawOrderProcessor(IOrderRepository orderRepo, IRawOrderRepository rawRepo, ILogger<RawOrderProcessor> logger)
    {
        _orderRepo = orderRepo;
        _rawRepo = rawRepo;
        _logger = logger;
    }

    public async Task ProcessPendingAsync(CancellationToken token)
    {
        while (true)
        {
            var unprocessed = await _rawRepo.GetUnprocessedAsync(100);

            if (!unprocessed.Any())
                break; // done, no more to process

            foreach (var record in unprocessed)
            {
                try
                {
                    var order = OrderMapper.MapFromRaw(record.Source, record.Id, record.Payload);
                    if (order == null) continue;

                    await _orderRepo.UpsertOrderAsync(order, token);

                    await _rawRepo.MarkProcessedAsync(record.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing record {Id}", record.Id);
                    await _rawRepo.MarkFailedAsync(record.Id, ex.Message); // <— new line
                }
            }
        }
    }

}