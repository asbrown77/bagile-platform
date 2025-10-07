using Bagile.EtlService.Collectors;
using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Bagile.IntegrationTests;

[TestFixture]
[Category("Integration")]
[Explicit("Requires valid WooCommerce credentials in appsettings.Development.json")]
public class WooCollectorIntegrationTests
{
    private WooCollector _collector = null!;

    [SetUp]
    public void Setup()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .Build();

        var client = new WooApiClient(new HttpClient(), config, NullLogger<WooApiClient>.Instance);
        _collector = new WooCollector(client, NullLogger<WooCollector>.Instance);
    }

    [Test]
    public async Task WooCollector_ShouldFetchRecentOrders()
    {
        var orders = await _collector.CollectAsync(null);
        orders.Should().NotBeNull();
        orders.Should().NotBeEmpty("because there should be recent orders in WooCommerce");
    }
}