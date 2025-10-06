using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

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

        var client = new XeroApiClient(new HttpClient(), config, NullLogger<XeroApiClient>.Instance);
        var result = await client.FetchInvoicesAsync(null, CancellationToken.None);

        result.Should().NotBeEmpty();
    }
}