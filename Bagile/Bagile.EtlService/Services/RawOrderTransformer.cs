using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.Infrastructure.Clients;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bagile.EtlService.Services
{
    public class RawOrderTransformer
    {
        private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(2);

        private readonly IRawOrderRepository _rawRepo;
        private readonly RawOrderRouter _router;
        private readonly IXeroApiClient _xeroClient;
        private readonly ILogger<RawOrderTransformer> _logger;

        public RawOrderTransformer(
            IRawOrderRepository rawRepo,
            RawOrderRouter router,
            ILogger<RawOrderTransformer> logger,
            IXeroApiClient xeroClient)
        {
            _rawRepo = rawRepo;
            _router = router;
            _logger = logger;
            _xeroClient = xeroClient;
        }

        public async Task ProcessPendingAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var batch = await _rawRepo.GetUnprocessedAsync(100);

                if (batch == null || !batch.Any())
                {
                    _logger.LogInformation("No unprocessed raw orders found.");
                    break;
                }

                _logger.LogInformation("Processing batch of {Count} raw orders.", batch.Count());

                foreach (var raw in batch)
                {
                    if (token.IsCancellationRequested)
                        return;

                    try
                    {
                        // First try to enrich Xero, skip routing if it succeeds
                        var enriched = await TryEnrichXeroWebhookAsync(raw, token);
                        if (enriched)
                            continue;

                        await _router.RouteAsync(raw, token);
                        await _rawRepo.MarkProcessedAsync(raw.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing raw order {RawOrderId}.", raw.Id);
                        await _rawRepo.MarkFailedAsync(raw.Id, ex.Message);
                    }
                }


                await Task.Delay(BatchDelay, token);
            }
        }

        private async Task<bool> TryEnrichXeroWebhookAsync(RawOrder rawOrder, CancellationToken token)
        {
            if (!rawOrder.Source.Equals("xero", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(rawOrder.Payload))
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

                _logger.LogInformation("Fetching invoice via resourceUrl = {Url}", resourceUrl);
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

                // Save the full invoice
                await _rawRepo.InsertAsync(
                    source: "xero",
                    externalId: invoiceId!,
                    payloadJson: fullInvoiceJson,
                    eventType: "invoice.import");

                await _rawRepo.MarkProcessedAsync(rawOrder.Id);

                _logger.LogInformation(
                    "Enriched Xero invoice {InvoiceId}, created new RawOrder entry. Old RawOrder {Id} processed.",
                    invoiceId, rawOrder.Id);

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


    }
}