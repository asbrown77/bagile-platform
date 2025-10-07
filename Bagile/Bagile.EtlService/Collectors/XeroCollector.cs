using Bagile.Infrastructure;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Filters;
using Bagile.Infrastructure.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Bagile.EtlService.Collectors
{
    public class XeroCollector : ISourceCollector
    {
        private readonly IXeroApiClient _xero;
        private readonly ILogger<XeroCollector> _logger;

        public string SourceName => "xero";

        public XeroCollector(IXeroApiClient xero, ILogger<XeroCollector> logger)
        {
            _xero = xero;
            _logger = logger;
        }

        // ✅ Convenience overload (no recursion)
        public Task<IEnumerable<string>> CollectAsync(CancellationToken ct = default)
            => CollectAsync(null, ct);

        public async Task<IEnumerable<string>> CollectAsync(DateTime? modifiedSince = null, CancellationToken ct = default)
        {
            _logger.LogInformation("Collecting Xero invoices...");

            var invoices = await _xero.FetchInvoicesAsync(modifiedSince, ct);
            var filtered = new List<string>();

            foreach (var json in invoices)
            {
                try
                {
                    var invoice = JsonSerializer.Deserialize<XeroInvoice>(json);
                    if (invoice != null && XeroInvoiceFilter.ShouldCapture(invoice))
                        filtered.Add(json);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse Xero invoice JSON");
                }
            }

            _logger.LogInformation("XeroCollector kept {Count} invoices", filtered.Count);
            return filtered;
        }
    }
}