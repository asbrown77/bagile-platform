using Bagile.EtlService.Utils;
using Bagile.Infrastructure;
using Bagile.Infrastructure.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ISourceCollector = Bagile.EtlService.Collectors.ISourceCollector;

namespace Bagile.EtlService.Services
{
    public class EtlRunner
    {
        private readonly IEnumerable<ISourceCollector> _collectors;
        private readonly IRawOrderRepository _repo;
        private readonly ILogger<EtlRunner> _logger;

        public EtlRunner(IEnumerable<ISourceCollector> collectors, IRawOrderRepository repo, ILogger<EtlRunner> logger)
        {
            _collectors = collectors;
            _repo = repo;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            foreach (var collector in _collectors)
            {
                var sourceName = collector.SourceName;
                _logger.LogInformation("Collecting from {Source}", sourceName);

                var modifiedSince = await _repo.GetLastTimestampAsync(sourceName);
                var payloads = await collector.CollectAsync(modifiedSince, ct);

                foreach (var raw in payloads)
                {
                    var id = JsonHelpers.ExtractId(raw);
                    await _repo.InsertIfChangedAsync(sourceName, id, raw, "etl.import");
                }
            }
        }

    }

}
