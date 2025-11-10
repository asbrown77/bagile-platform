using Bagile.Domain.Repositories;
using Bagile.EtlService.Services;
using Bagile.EtlService.Collectors;
using Bagile.Infrastructure;
using Bagile.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

[TestFixture]
public class EtlRunnerTests
{
    [Test]
    public async Task EtlRunner_Inserts_All_Collected_Data()
    {
        var woo = new Mock<ISourceCollector>();
        woo.Setup(c => c.SourceName).Returns("woo");
        woo.Setup(c => c.CollectOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { """{"id":1}""" });

        var xero = new Mock<ISourceCollector>();
        xero.Setup(c => c.SourceName).Returns("xero");
        xero.Setup(c => c.CollectOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { """{"id":99}""" });

        var repo = new Mock<IRawOrderRepository>();

        // new constructor: orders, products, repo, logger
        var runner = new EtlRunner(
            new[] { woo.Object, xero.Object },
            Enumerable.Empty<IProductCollector>(),
            repo.Object,
            NullLogger<EtlRunner>.Instance);

        await runner.RunAsync();

        repo.Verify(r => r.InsertIfChangedAsync("woo", "1", It.IsAny<string>(), "import"), Times.Once);
        repo.Verify(r => r.InsertIfChangedAsync("xero", "99", It.IsAny<string>(), "import"), Times.Once);
    }

}