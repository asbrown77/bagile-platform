using Bagile.EtlService.Collectors;
using Bagile.EtlService.Projectors;
using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System.Net.Http;

namespace Bagile.IntegrationTests.Api;

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

        // We don’t actually care about the projector logic in this integration test
        var dummyProjector = new DummyImporter();

        var client = new WooApiClient(new HttpClient(), config, NullLogger<WooApiClient>.Instance);
        _collector = new WooOrderCollector(client, dummyProjector, NullLogger<WooOrderCollector>.Instance);
    }

    [Test]
    public async Task WooCollector_ShouldFetchRecentOrders()
    {
        var orders = await _collector.CollectOrdersAsync(null);

        orders.Should().NotBeNull();
        orders.Should().NotBeEmpty("because there should be recent orders in WooCommerce");
    }

    private class DummyImporter : WooCourseImporter
    {
        // Minimal constructor match (no real logic)
        public DummyImporter() : base(null!, null!, NullLogger<WooCourseImporter>.Instance) { }
    }
}