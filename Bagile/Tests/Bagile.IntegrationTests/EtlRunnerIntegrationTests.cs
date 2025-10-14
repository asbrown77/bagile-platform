using System.Text.Json;
using Bagile.EtlService.Collectors;
using Bagile.EtlService.Services;
using Bagile.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Bagile.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class EtlRunnerIntegrationTests
{
    [Test]
    public async Task RunAsync_Should_Collect_And_Insert_From_All_Sources()
    {
        // Arrange: two mock collectors
        var wooCollector = new Mock<ISourceCollector>();
        wooCollector.Setup(c => c.SourceName).Returns("woo");
        wooCollector.Setup(c => c.CollectOrdersAsync(It.IsAny<DateTime?>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { """{"id": 1001}""" });

        var xeroCollector = new Mock<ISourceCollector>();
        xeroCollector.Setup(c => c.SourceName).Returns("xero");
        xeroCollector.Setup(c => c.CollectOrdersAsync(It.IsAny<DateTime?>(),It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { """{"id": 2002}""" });

        var repo = new RawOrderRepository($"{DatabaseFixture.ConnectionString};SearchPath=bagile");

        var runner = new EtlRunner(
            new[] { wooCollector.Object, xeroCollector.Object },          // order collectors
            Enumerable.Empty<IProductCollector>(),                         // no product collectors
            repo,
            NullLogger<EtlRunner>.Instance);


        // Act
        await runner.RunAsync();

        // Assert
        var all = await repo.GetAllAsync();

        var ids = all
            .Select(r => JsonDocument.Parse(r.Payload).RootElement.GetProperty("id").GetInt32())
            .ToList();

        ids.Count(i => i == 1001).Should().Be(1);
        ids.Count(i => i == 2002).Should().Be(1);
    }
}