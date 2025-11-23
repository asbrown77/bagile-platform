using Bagile.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Bagile.EtlService.Services
{
    public class RawOrderProcessor
    {
        private static readonly TimeSpan BatchDelay = TimeSpan.FromSeconds(2);

        private readonly IRawOrderRepository _rawRepo;
        private readonly RawOrderRouter _router;
        private readonly ILogger<RawOrderProcessor> _logger;

        public RawOrderProcessor(
            IRawOrderRepository rawRepo,
            RawOrderRouter router,
            ILogger<RawOrderProcessor>? logger = null)
        {
            _rawRepo = rawRepo;
            _router = router;
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<RawOrderProcessor>.Instance;
        }

        public async Task ProcessPendingAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var batch = await _rawRepo.GetUnprocessedAsync(100);

                if (!batch.Any())
                {
                    _logger.LogInformation("No unprocessed raw orders found.");
                    break;
                }

                _logger.LogInformation("Processing batch of {Count} raw orders...", batch.Count());

                foreach (var raw in batch)
                {
                    await _router.RouteAsync(raw, token);
                }

                await Task.Delay(BatchDelay, token);
            }
        }
    }
}