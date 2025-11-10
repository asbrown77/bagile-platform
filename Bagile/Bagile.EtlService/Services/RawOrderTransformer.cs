using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Mappers;
using Bagile.EtlService.Models;
using Bagile.Infrastructure.Clients;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Services
{
    public class RawOrderTransformer
    {
        private const string WooSource = "woo";
        private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(2);

        private readonly IOrderRepository _orderRepo;
        private readonly IRawOrderRepository _rawRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IEnrolmentRepository _enrolmentRepo;
        private readonly ICourseScheduleRepository _courseRepo;
        private readonly IFooEventsTicketsClient _fooEventsClient;
        private readonly ILogger<RawOrderTransformer> _logger;

        private readonly WooOrderHandler _wooHandler;

        public RawOrderTransformer(
            IOrderRepository orderRepo,
            IRawOrderRepository rawRepo,
            IStudentRepository studentRepo,
            IEnrolmentRepository enrolmentRepo,
            ICourseScheduleRepository courseRepo,
            IFooEventsTicketsClient fooEventsClient,
            ILogger<RawOrderTransformer> logger)
        {
            _orderRepo = orderRepo;
            _rawRepo = rawRepo;
            _studentRepo = studentRepo;
            _enrolmentRepo = enrolmentRepo;
            _courseRepo = courseRepo;
            _fooEventsClient = fooEventsClient;
            _logger = logger;

            // Delegate all Woo-specific logic into a dedicated handler
            _wooHandler = new WooOrderHandler(
                _studentRepo,
                _enrolmentRepo,
                _courseRepo,
                _fooEventsClient,
                logger);
        }

        public async Task ProcessPendingAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var unprocessed = await _rawRepo.GetUnprocessedAsync(100);

                if (!unprocessed.Any())
                {
                    _logger.LogInformation("No unprocessed raw orders found. All done.");
                    break;
                }

                _logger.LogInformation("Processing batch of {Count} raw orders...", unprocessed.Count());

                foreach (var rawOrder in unprocessed)
                {
                    await ProcessSingleOrderAsync(rawOrder, token);
                }

                await Task.Delay(BatchDelay, token);
            }
        }

        private async Task ProcessSingleOrderAsync(RawOrder rawOrder, CancellationToken token)
        {
            try
            {
                var order = OrderMapper.MapFromRaw(rawOrder);
                if (order == null)
                {
                    _logger.LogInformation(
                        "Skipping non actionable RawOrder {Id} from source {Source} with event type {EventType}.",
                        rawOrder.Id,
                        rawOrder.Source,
                        rawOrder.EventType);

                    await _rawRepo.MarkProcessedAsync(rawOrder.Id);
                    return;
                }

                var orderId = await _orderRepo.UpsertOrderAsync(order, token);

                if (rawOrder.Source.Equals(WooSource, StringComparison.OrdinalIgnoreCase))
                {
                    await _wooHandler.HandleAsync(rawOrder, orderId);
                }

                await ValidateEnrolmentConsistencyAsync(order, orderId);

                await _rawRepo.MarkProcessedAsync(rawOrder.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RawOrder {Id}", rawOrder.Id);
                await _rawRepo.MarkFailedAsync(rawOrder.Id, ex.Message);
            }
        }

        private async Task ValidateEnrolmentConsistencyAsync(Order order, long orderId)
        {
            if (!order.TotalQuantity.HasValue)
                return;

            var enrolmentCount = await _enrolmentRepo.CountByOrderIdAsync(orderId);

            if (enrolmentCount < order.TotalQuantity.Value)
            {
                _logger.LogWarning(
                    "Order {OrderId}, {Name}, expected {ExpectedQty} attendees, found only {ActualQty}.",
                    orderId,
                    order.BillingCompany,
                    order.TotalQuantity,
                    enrolmentCount
                );
            }
        }
    }
}
