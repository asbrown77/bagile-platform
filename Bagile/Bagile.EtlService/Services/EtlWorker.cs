using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.EtlService.Services
{
    public class EtlWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EtlWorker> _logger;

        public EtlWorker(IServiceScopeFactory scopeFactory, ILogger<EtlWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Starting ETL cycle at {Time}", DateTimeOffset.Now);

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var runner = scope.ServiceProvider.GetRequiredService<EtlRunner>();
                    await runner.RunAsync(stoppingToken);

                    var processor = scope.ServiceProvider.GetRequiredService<RawOrderProcessor>();
                    await processor.ProcessPendingAsync(stoppingToken);

                    _logger.LogInformation("ETL + RawOrder processing cycle completed successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ETL cycle failed");
                }

                _logger.LogInformation("ETL cycle complete, sleeping...");
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }
}

