using Bagile.EtlService.Collectors;
using Bagile.EtlService.Projectors;
using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Bagile.IntegrationTests;

[TestFixture]
[Category("Integration")]
[Explicit("Requires valid WooCommerce credentials in appsettings.Development.json")]
public class WooCollectorIntegrationTests
{
    private WooOrderCollector _collector = null!;

    [SetUp]
    public void Setup()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .Build();

        var mockProjector = new Mock<WooCourseImporter>(null!, null!, null!, NullLogger<WooCourseImporter>.Instance);

        var client = new WooApiClient(new HttpClient(), config, NullLogger<WooApiClient>.Instance);
        _collector = new WooOrderCollector(client, mockProjector.Object, NullLogger<WooOrderCollector>.Instance);
    }

    [Test]
    public async Task WooCollector_ShouldFetchRecentOrders()
    {
        var orders = await _collector.CollectOrdersAsync(null);
        orders.Should().NotBeNull();
        orders.Should().NotBeEmpty("because there should be recent orders in WooCommerce");
    }
}