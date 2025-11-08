using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using System.Net;
using System.Text;

namespace Bagile.UnitTests.Clients;

[TestFixture]
public class FooEventsTicketsClientTests
{
    private Mock<HttpMessageHandler> _mockHttp;
    private IFooEventsTicketsClient _client;

    [SetUp]
    public void Setup()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["WordPress:BaseUrl"] = "https://test.bagile.co.uk",
                ["WordPress:BagileApiKey"] = "test-key"
            })
            .Build();

        _mockHttp = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttp.Object);

        _client = new FooEventsTicketsClient(
            httpClient,
            config,
            NullLogger<FooEventsTicketsClient>.Instance);
    }

    [Test]
    public async Task FetchTicketsForOrderAsync_ShouldReturnTickets_WhenFound()
    {
        // Arrange
        var json = """
                   {
                     "order_id": 12345,
                     "currency": "GBP",
                     "tickets": [
                       {
                         "ticket_id": 1,
                         "ticket_number": "7",
                         "status": "Paid",
                         "event_id": 11492,
                         "event_name": "Professional Scrum Product Owner\u2122 - 12-13 Nov 25",
                         "event_start": "2025-11-12T00:00:00+00:00",
                         "attendee_name": "Test Person",
                         "attendee_email": "test@example.com",
                         "designation": "Transfer from cancelled PSM-061125-CB",
                         "meta": {}
                       }
                     ]
                   }
                   """;

        _mockHttp.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        // Act
        var tickets = await _client.FetchTicketsForOrderAsync("12345");

        // Assert
        tickets.Should().HaveCount(1);
        var ticket = tickets.First();
        ticket.AttendeeEmail.Should().Be("test@example.com");
        ticket.Designation.Should().Contain("Transfer from cancelled");
        ticket.Status.Should().Be("Paid");
    }

    [Test]
    public async Task FetchTicketsForOrderAsync_ShouldReturnEmpty_When404()
    {
        // Arrange: API returns 404
        _mockHttp.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var tickets = await _client.FetchTicketsForOrderAsync("99999");

        // Assert
        tickets.Should().BeEmpty();
    }

    [Test]
    public async Task FetchTicketsForOrderAsync_ShouldReturnEmpty_WhenTicketsArrayMissing()
    {
        // Arrange: API returns object missing "tickets"
        var json = """
        {
          "order_id": 12345,
          "currency": "GBP"
        }
        """;

        _mockHttp.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });

        // Act
        var tickets = await _client.FetchTicketsForOrderAsync("12345");

        // Assert
        tickets.Should().BeEmpty();
    }
}
