using System;
using System.Linq;
using System.Text.Json;
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
        private const string XeroSource = "xero";
        private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(2);

        private readonly IOrderRepository _orderRepo;
        private readonly IRawOrderRepository _rawRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IEnrolmentRepository _enrolmentRepo;
        private readonly ICourseScheduleRepository _courseRepo;
        private readonly IFooEventsTicketsClient _fooEventsClient;
        private readonly IXeroApiClient _xeroClient;
        private readonly ILogger<RawOrderTransformer> _logger;

        private readonly WooOrderHandler _wooHandler;

        public RawOrderTransformer(
            IOrderRepository orderRepo,
            IRawOrderRepository rawRepo,
            IStudentRepository studentRepo,
            IEnrolmentRepository enrolmentRepo,
            ICourseScheduleRepository courseRepo,
            IFooEventsTicketsClient fooEventsClient,
            IXeroApiClient xeroClient,
            ILogger<RawOrderTransformer> logger)
        {
            _orderRepo = orderRepo;
            _rawRepo = rawRepo;
            _studentRepo = studentRepo;
            _enrolmentRepo = enrolmentRepo;
            _courseRepo = courseRepo;
            _fooEventsClient = fooEventsClient;
            _xeroClient = xeroClient;
            _logger = logger;

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
                token.ThrowIfCancellationRequested();

                // Handle Xero webhook envelopes first
                if (await TryEnrichXeroWebhookAsync(rawOrder, token))
                    return; // handled, now a new full invoice exists in raw_orders

                // Normal order mapping flow
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
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Processing of RawOrder {Id} was canceled by token.", rawOrder.Id);
                return;
            }
            catch (XeroRateLimitException ex)
            {
                _logger.LogWarning(ex, "Rate limited by Xero while processing RawOrder {Id}. Leaving as pending.", rawOrder.Id);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing RawOrder {Id}", rawOrder.Id);
                await _rawRepo.MarkFailedAsync(rawOrder.Id, ex.Message);
            }
        }

        private async Task<bool> TryEnrichXeroWebhookAsync(RawOrder rawOrder, CancellationToken token)
        {
            if (!rawOrder.Source.Equals(XeroSource, StringComparison.OrdinalIgnoreCase))
                return false;

            // Quick shape check for webhook envelope
            if (string.IsNullOrWhiteSpace(rawOrder.Payload) ||
                !rawOrder.Payload.Contains("\"resourceUrl\"", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                using var doc = JsonDocument.Parse(rawOrder.Payload);
                var root = doc.RootElement;

                if (!root.TryGetProperty("events", out var events) || events.GetArrayLength() == 0)
                    return false;

                var evt = events[0];
                if (!evt.TryGetProperty("resourceUrl", out var urlProp))
                    return false;

                var resourceUrl = urlProp.GetString();
                if (string.IsNullOrWhiteSpace(resourceUrl))
                    return false;

                _logger.LogInformation("Fetching full Xero invoice for RawOrder {Id} from {Url}", rawOrder.Id, resourceUrl);

                var fullInvoiceJson = await _xeroClient.GetInvoiceByUrlAsync(resourceUrl, token);
                if (string.IsNullOrWhiteSpace(fullInvoiceJson))
                {
                    _logger.LogWarning("No invoice returned for {Url}", resourceUrl);
                    return false;
                }

                var invoiceId = ExtractInvoiceId(fullInvoiceJson);
                if (string.IsNullOrWhiteSpace(invoiceId))
                {
                    _logger.LogWarning("Could not extract InvoiceID from Xero invoice fetched for RawOrder {Id}", rawOrder.Id);
                    return false;
                }

                await _rawRepo.InsertAsync(
                    source: XeroSource,
                    externalId: invoiceId,
                    payloadJson: fullInvoiceJson,
                    eventType: "invoice.import");

                await _rawRepo.MarkProcessedAsync(rawOrder.Id);
                _logger.LogInformation("Inserted enriched Xero invoice from webhook {RawId}", rawOrder.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enrich Xero webhook RawOrder {Id}", rawOrder.Id);
                await _rawRepo.MarkFailedAsync(rawOrder.Id, ex.Message);
                return false;
            }
        }

        private static string ExtractInvoiceId(string invoiceJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(invoiceJson);
                if (doc.RootElement.TryGetProperty("Invoices", out var invoices) &&
                    invoices.ValueKind == JsonValueKind.Array &&
                    invoices.GetArrayLength() > 0)
                {
                    var invoice = invoices[0];
                    if (invoice.TryGetProperty("InvoiceID", out var idProp))
                        return idProp.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // ignored
            }

            return string.Empty;
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
