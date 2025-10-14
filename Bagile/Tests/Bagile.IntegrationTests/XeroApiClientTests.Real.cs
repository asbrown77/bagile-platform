using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bagile.IntegrationTests;

[TestFixture]
[Category("Integration")]
[Explicit("Runs against live Xero API. Requires valid Xero access token in appsettings.Development.json.")]
public class XeroApiClientTests
{
    private IConfiguration _config = null!;
    private XeroApiClient _client = null!;

    [SetUp]
    public void Setup()
    {
        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .Build();

        var http = new HttpClient();
        var auth = new XeroTokenRefreshService(http, _config, NullLogger<XeroTokenRefreshService>.Instance);

        _client = new XeroApiClient(http, _config, NullLogger<XeroApiClient>.Instance, auth);
    }

    [Test]
    public async Task FetchInvoicesAsync_ShouldReturnInvoices()
    {
        var result = await _client.FetchInvoicesAsync(null, CancellationToken.None);

        result.Should().NotBeNull("the Xero API should respond if credentials are valid");
        result.Should().NotBeEmpty("because there should be at least one invoice available");
    }
}
