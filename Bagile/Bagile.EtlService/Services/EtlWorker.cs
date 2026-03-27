using Bagile.EtlService.Models;
using Microsoft.Extensions.Options;

namespace Bagile.EtlService.Services
{
    public class EtlWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EtlWorker> _logger;
        private readonly EtlOptions _options;

        public EtlWorker(
            IServiceScopeFactory scopeFactory,
            ILogger<EtlWorker> logger,
            IOptions<EtlOptions> options)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting ETL cycle at {Time}", DateTimeOffset.Now);

                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var runner = scope.ServiceProvider.GetRequiredService<SourceDataImporter>();
                    await runner.RunAsync(stoppingToken);

                    var processor = scope.ServiceProvider.GetRequiredService<RawOrderTransformer>();

                    await processor.ProcessPendingAsync(stoppingToken);

                    _logger.LogInformation("ETL + RawOrder processing cycle completed. Pending raw orders exhausted.");

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ETL cycle failed");
                }

                _logger.LogInformation("ETL cycle complete, sleeping for {Minutes} minutes...", _options.IntervalMinutes);
                await Task.Delay(TimeSpan.FromMinutes(_options.IntervalMinutes), stoppingToken);
            }
        }
    }
}

