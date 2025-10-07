using Bagile.EtlService.Collectors;
using Bagile.Infrastructure;
using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

[TestFixture]
public class WooCollectorTests
{
    [Test]
    public async Task CollectAsync_Returns_Orders_From_ApiClient()
    {
        var mockApi = new Mock<IWooApiClient>();
        mockApi.Setup(x => x.FetchOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { """{"id":1}""" });

        var collector = new WooCollector(mockApi.Object, NullLogger<WooCollector>.Instance);
        var result = await collector.CollectAsync(null);

        result.Should().HaveCount(1);
        mockApi.Verify(x => x.FetchOrdersAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}