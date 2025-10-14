using System.Text.Json;
using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Bagile.IntegrationTests;

[TestFixture]
[Explicit("Runs against live WooCommerce API. Requires valid credentials in appsettings.Development.json.")]
public class WooApiClientTests
{
    private IWooApiClient _client = null!;

    [SetUp]
    public void Setup()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var httpClient = new HttpClient();
        _client = new WooApiClient(httpClient, config, NullLogger<WooApiClient>.Instance);
    }

    [Test]
    public async Task FetchOrdersAsync_Returns_At_Least_One_Order()
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var orders = await _client.FetchOrdersAsync(since, CancellationToken.None);

        orders.Should().NotBeNull();
        orders.Should().NotBeEmpty("because Woo should return at least one order in the last 7 days");
    }


    [Test]
    public async Task FetchProductsAsync_Should_Print_First_Product_With_MetaData()
    {
        var products = await _client.FetchProductsAsync(CancellationToken.None);
        var first = products.FirstOrDefault();

        Console.WriteLine(JsonSerializer.Serialize(first, new JsonSerializerOptions { WriteIndented = true }));
    }

}
