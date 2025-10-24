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
        var unprocessed = await _rawRepo.GetUnprocessedAsync(100);

        foreach (var record in unprocessed)
        {
            try
            {
                var order = OrderMapper.MapFromRaw(record.Source, record.Id, record.Payload);
                if (order == null)
                {
                    _logger.LogWarning("Unknown source {Source} for record {Id}", record.Source, record.Id);
                    continue;
                }

                await _orderRepo.UpsertOrderAsync(
                    order.RawOrderId,
                    order.ExternalId,
                    order.Source,
                    order.Type,
                    order.BillingCompany,
                    order.ContactName,
                    order.ContactEmail,
                    order.TotalAmount,
                    order.Status,
                    order.OrderDate,
                    token);

                await _rawRepo.MarkProcessedAsync(record.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing raw record {Id}", record.Id);
            }
        }
    }
}