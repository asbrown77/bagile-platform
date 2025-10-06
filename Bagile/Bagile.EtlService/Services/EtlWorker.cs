using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.EtlService.Services
{
    public class EtlWorker : BackgroundService
    {
        private readonly EtlRunner _runner;
        private readonly ILogger<EtlWorker> _logger;

        public EtlWorker(EtlRunner runner, ILogger<EtlWorker> logger)
        {
            _runner = runner;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ETL Worker started");

            while (!stoppingToken.IsCancellationRequested)
            {
                await _runner.RunAsync(stoppingToken);
                _logger.LogInformation("ETL cycle complete");
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }
    }

}
