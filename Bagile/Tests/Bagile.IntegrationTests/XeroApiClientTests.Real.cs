using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using static System.Net.WebRequestMethods;

[TestFixture]
[Category("Integration")]
[Explicit("Runs against live Xero API. Requires valid Xero access token in appsettings.Development.json.")]
public class XeroApiClientTests
{
    [Test]
    public async Task FetchInvoicesAsync_ShouldReturnInvoices()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .Build();

        var http = new HttpClient();
        var auth = new XeroAuthService(http, config, NullLogger<XeroAuthService>.Instance);

        var client = new XeroApiClient(new HttpClient(), config, NullLogger<XeroApiClient>.Instance, auth);
        var result = await client.FetchInvoicesAsync(null, CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}