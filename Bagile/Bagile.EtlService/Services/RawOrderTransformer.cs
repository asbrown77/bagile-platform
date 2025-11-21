using System.Text.Json;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Mappers;
using Bagile.Infrastructure.Clients;

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

        private bool IsTransferOrder(string payload)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // A transfer order ALWAYS has line_items
            if (!root.TryGetProperty("line_items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return false;

            // If WooCommerceEventsOrderTickets exists, this is NOT a transfer
            if (root.TryGetProperty("meta_data", out var meta) &&
                meta.EnumerateArray().Any(m =>
                    m.TryGetProperty("key", out var key) &&
                    key.GetString() == "WooCommerceEventsOrderTickets"))
                return false;

            // Otherwise this is a transfer
            return true;
        }


        private async Task ProcessSingleOrderAsync(RawOrder rawOrder, CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                // 1. Xero webhook envelopes
                if (await TryEnrichXeroWebhookAsync(rawOrder, token))
                    return;

                // 2. Woo transfer orders, handled by our custom path
                if (rawOrder.Source.Equals(WooSource, StringComparison.OrdinalIgnoreCase)
                    && IsTransferOrder(rawOrder.Payload))
                {
                    var handled = await HandleTransferOrderAsync(rawOrder);

                    if (handled)
                    {
                        await _rawRepo.MarkProcessedAsync(rawOrder.Id);
                        return;
                    }

                    // If not handled, fall through to normal mapping
                    _logger.LogWarning(
                        "Transfer detection for RawOrder {Id} failed to handle. Falling back to normal mapping.",
                        rawOrder.Id);
                }

                // 3. Normal order mapping
                var order = OrderMapper.MapFromRaw(rawOrder);
                if (order == null)
                {
                    _logger.LogInformation("Skipping RawOrder {Id}. Not actionable.", rawOrder.Id);
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
            catch (XeroNotFoundException ex)
            {
                _logger.LogWarning(ex,
                    "Xero resource not found for RawOrder {Id}. Marking as error (will not retry).",
                    rawOrder.Id);

                await _rawRepo.MarkFailedAsync(rawOrder.Id, ex.Message);
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

        private (string email, string first, string last, string company, string sku)
            ExtractTransferFallbackData(string payload)
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            // Billing block: always present in normal Woo orders
            if (!root.TryGetProperty("billing", out var billing) ||
                billing.ValueKind != JsonValueKind.Object)
            {
                return ("", "", "", "", "");
            }

            string email = billing.TryGetProperty("email", out var emailProp)
                ? emailProp.GetString() ?? ""
                : "";

            string first = billing.TryGetProperty("first_name", out var fProp)
                ? fProp.GetString() ?? ""
                : "";

            string last = billing.TryGetProperty("last_name", out var lProp)
                ? lProp.GetString() ?? ""
                : "";

            string company = billing.TryGetProperty("company", out var compProp)
                ? compProp.GetString() ?? ""
                : "";

            string sku = "";
            if (root.TryGetProperty("line_items", out var items) &&
                items.ValueKind == JsonValueKind.Array &&
                items.GetArrayLength() > 0)
            {
                var firstItem = items[0];
                if (firstItem.TryGetProperty("sku", out var skuProp))
                    sku = skuProp.GetString() ?? "";
            }

            return (email, first, last, company, sku);
        }


        private async Task<bool> HandleTransferOrderAsync(RawOrder rawOrder)
{
    try
    {
        var (email, first, last, company, sku) = ExtractTransferFallbackData(rawOrder.Payload);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(sku))
        {
            _logger.LogWarning("Transfer detection. missing email or sku on RawOrder {Id}", rawOrder.Id);
            return false;
        }

        // 1. Extract Woo number and date_created so we behave like normal mapping
        string externalId = rawOrder.ExternalId;
        DateTime orderDate = rawOrder.CreatedAt;

        using (var doc = JsonDocument.Parse(rawOrder.Payload))
        {
            var root = doc.RootElement;

            if (root.TryGetProperty("number", out var numProp))
            {
                var num = numProp.GetString();
                if (!string.IsNullOrWhiteSpace(num))
                    externalId = num;
            }

            if (root.TryGetProperty("date_created", out var dateProp))
            {
                var dateStr = dateProp.GetString();
                if (!string.IsNullOrWhiteSpace(dateStr) &&
                    DateTime.TryParse(dateStr, out var parsed))
                {
                    orderDate = parsed;
                }
            }
        }

        // 2. Resolve schedule by SKU, but allow null for invalid SKU
        long? scheduleId = await _courseRepo.GetIdBySkuAsync(sku);
        if (!scheduleId.HasValue)
        {
            _logger.LogWarning("Transfer detection. no schedule found for SKU {Sku} on RawOrder {Id}", sku, rawOrder.Id);
        }

        // 3. Upsert student
        var studentId = await _studentRepo.UpsertAsync(new Student
        {
            Email = email,
            FirstName = first,
            LastName = last,
            Company = company
        });

        // 4. Create new order using Woo number as external id
        var order = new Order
        {
            RawOrderId = rawOrder.Id,
            ExternalId = externalId,
            ContactEmail = email,
            ContactName = $"{first} {last}",
            BillingCompany = company,
            Source = WooSource,
            Status = "completed",
            TotalQuantity = 1,
            OrderDate = orderDate
        };

        var newOrderId = await _orderRepo.UpsertOrderAsync(order, CancellationToken.None);

        // 5. Try find previous active enrolment for this student
        var oldEnrol = await _enrolmentRepo.FindActiveByStudentEmailAsync(email);

        long newEnrolId;

        if (oldEnrol != null)
        {
            // 6a. New enrolment linked to previous one
            newEnrolId = await _enrolmentRepo.InsertAsync(new Enrolment
            {
                OrderId = newOrderId,
                StudentId = studentId,
                CourseScheduleId = scheduleId,
                Status = "active",
                TransferredFromEnrolmentId = oldEnrol.Id,
                TransferReason = "transfer_detected"
            });

            await _enrolmentRepo.MarkTransferredAsync(oldEnrol.Id, newEnrolId);
        }
        else
        {
            // 6b. No previous enrolment, still create one
            newEnrolId = await _enrolmentRepo.InsertAsync(new Enrolment
            {
                OrderId = newOrderId,
                StudentId = studentId,
                CourseScheduleId = scheduleId,
                Status = "active",
                TransferReason = "transfer_detected_no_previous"
            });
        }

        _logger.LogInformation(
            "Handled transfer RawOrder {Id}. New order {OrderExternalId}, new enrolment {EnrolId}, previous enrolment {OldEnrolId}",
            rawOrder.Id,
            externalId,
            newEnrolId,
            oldEnrol?.Id);

        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in HandleTransferOrderAsync for RawOrder {Id}", rawOrder.Id);
        return false;
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
