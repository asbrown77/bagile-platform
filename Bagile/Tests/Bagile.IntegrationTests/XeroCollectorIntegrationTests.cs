using Bagile.EtlService.Collectors;
using Bagile.Infrastructure.Clients;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Bagile.IntegrationTests;

[TestFixture]
[Category("Integration")]
[Explicit("Requires valid Xero OAuth2 credentials in appsettings.Development.json")]
public class XeroCollectorIntegrationTests
{
    private XeroCollector _collector = null!;

    [SetUp]
    public void Setup()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .Build();

        var http = new HttpClient();
        var auth = new XeroTokenRefreshService(http, config, NullLogger<XeroTokenRefreshService>.Instance);

        var client = new XeroApiClient(new HttpClient(), config, NullLogger<XeroApiClient>.Instance, auth);
        _collector = new XeroCollector(client, NullLogger<XeroCollector>.Instance);
    }

    [Test]
    public async Task XeroCollector_ShouldFetchAndFilterInvoices()
    {
        var invoices = await _collector.CollectAsync(null);
        invoices.Should().NotBeNull();

        // Optional: if you know at least one should be AUTHORISED or PAID
        invoices.Should().OnlyContain(i => i.Contains("AUTHORISED") || i.Contains("PAID"));
    }
}