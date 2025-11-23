using Bagile.Domain.Entities;
using Bagile.EtlService.Services;
using Bagile.Infrastructure.Clients;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Text.Json;

namespace Bagile.UnitTests.EtlService;

[TestFixture]
public class WooOrderParserTests
{
    private WooOrderParser _parser = null!;

    [SetUp]
    public void Setup()
    {
        var mockLogger = Mock.Of<ILogger<WooOrderParser>>();
        var mockFooEvents = Mock.Of<IFooEventsTicketsClient>();

        _parser = new WooOrderParser(mockLogger, mockFooEvents);
    }

    [Test]
    public async Task Parse_ShouldPopulateBasicFields()
    {
        var json = @"{
        ""id"": 123,
        ""billing"": {
            ""first_name"": ""John"",
            ""last_name"": ""Doe"",
            ""email"": ""john@test.com"",
            ""company"": ""TestCo""
        },
        ""line_items"": [
            { ""product_id"": 11840, ""sku"": ""PSPO-010125-AB"" }
        ]
    }";

        var dto = await _parser.Parse(
            new RawOrder { Id = 1, ExternalId = "123", Source = "woo", Payload = json }
        );

        Assert.That(dto.ExternalId, Is.EqualTo("123"));
        Assert.That(dto.BillingEmail, Is.EqualTo("john@test.com"));
        Assert.That(dto.BillingName, Is.EqualTo("John Doe"));
        Assert.That(dto.Tickets.Count, Is.EqualTo(1));
        Assert.That(dto.Tickets[0].Sku, Is.EqualTo("PSPO-010125-AB"));
    }

    [Test]
    public async Task Parse_NoBillingProvided_ShouldStillWork()
    {
        var json = @"{
        ""id"": 123,
        ""billing"": {},
        ""line_items"": []
    }"; 

        var dto = await _parser.Parse(
            new RawOrder { Id = 1, ExternalId = "123", Source = "woo", Payload = json }
        );

        Assert.That(dto.BillingEmail, Is.EqualTo(""));
        Assert.That(dto.Tickets, Is.Empty);
    }

    // [Test]
    // public void Parse_InvalidJson_ShouldThrow()
    // {
    //     Assert.Throws<System.Text.Json.JsonReaderException> (() =>
    //         _parser.Parse(
    //             new RawOrder { Id = 1, Payload = "{ invalid json", Source = "woo" },
    //             1
    //         ));
    //
    //
    // }

}
