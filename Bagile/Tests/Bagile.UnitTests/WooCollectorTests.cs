using Bagile.EtlService.Collectors;
using Bagile.EtlService.Projectors;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Bagile.UnitTests.Collectors;

[TestFixture]
public class WooCollectorTests
{
    private Mock<IWooApiClient> _mockApi;
    private WooOrderCollector _collector;
    private Mock<IImporter<WooProductDto>> _mockProjector;

    [SetUp]
    public void Setup()
    {
        _mockApi = new Mock<IWooApiClient>();
        _mockProjector = new Mock<IImporter<WooProductDto>>();
        _collector = new WooOrderCollector(_mockApi.Object,_mockProjector.Object, NullLogger<WooOrderCollector>.Instance);
    }

    [Test]
    public async Task CollectAsync_Should_Return_Single_Page()
    {
        _mockApi.Setup(x => x.FetchOrdersAsync(1, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { """{"id":1}""" });
        _mockApi.Setup(x => x.FetchOrdersAsync(2, 100, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var result = await _collector.CollectOrdersAsync();

        result.Should().HaveCount(1);
        _mockApi.Verify(x => x.FetchOrdersAsync(1, 100, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockApi.Verify(x => x.FetchOrdersAsync(2, 100, null, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CollectAsync_Should_Continue_When_Page_Is_Full()
    {
        _mockApi.Setup(x => x.FetchOrdersAsync(1, 100, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Range(1, 100).Select(i => $@"{{""id"":{i}}}").ToList());
        _mockApi.Setup(x => x.FetchOrdersAsync(2, 100, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string> { """{"id":101}""" });

        var result = await _collector.CollectOrdersAsync();

        result.Should().HaveCount(101);
        _mockApi.Verify(x => x.FetchOrdersAsync(1, 100, null, It.IsAny<CancellationToken>()), Times.Once);
        _mockApi.Verify(x => x.FetchOrdersAsync(2, 100, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CollectAsync_Should_Stop_When_No_Orders()
    {
        _mockApi.Setup(x => x.FetchOrdersAsync(1, 100, null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>());

        var result = await _collector.CollectOrdersAsync();

        result.Should().BeEmpty();
        _mockApi.Verify(x => x.FetchOrdersAsync(1, 100, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CollectAsync_Should_Support_Cancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = async () => await _collector.CollectOrdersAsync(null, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
