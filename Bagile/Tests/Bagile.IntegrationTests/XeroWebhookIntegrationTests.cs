using Bagile.Infrastructure.Models;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bagile.Infrastructure.Clients;

[TestFixture]
[Category("Integration")]
public class XeroWebhookIntegrationTests
{
    private WebApplicationFactory<Program> _factory;
    private HttpClient _client;
    private string _webhookSecret = "testsecret";
    private NpgsqlConnection _db;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var connStr = DatabaseFixture.ConnectionString;
        _db = new NpgsqlConnection(connStr);
        await _db.OpenAsync();

        await _db.ExecuteAsync("DELETE FROM bagile.raw_orders;");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    var dict = new Dictionary<string, string>
                    {
                        ["ConnectionStrings:DefaultConnection"] = connStr,
                        ["Xero:WebhookSecret"] = _webhookSecret
                    };
                    config.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    // remove the real client if registered
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IXeroApiClient));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    // register fake
                    services.AddSingleton<IXeroApiClient, FakeXeroApiClient>();
                });
            });

        _client = _factory.CreateClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _db?.Dispose();
        _client?.Dispose();
        _factory?.Dispose();
    }

    [Test]
    public async Task XeroWebhook_Post_ValidPayload_UpsertsRawOrder()
    {
        // Arrange: minimal valid Xero webhook payload
        var obj = new
        {
            events = new[]
            {
                new { resourceId = "abc-123", eventCategory = "INVOICE", eventType = "UPDATE" }
            }
        };

        var payload = JsonSerializer.Serialize(obj);
        var signature = ComputeHmacSha256Base64(payload, _webhookSecret);

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/xero")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-xero-signature", signature);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = (await _db.QueryAsync<RawOrder>(
            "SELECT * FROM bagile.raw_orders WHERE external_id = @id", new { id = "abc-123" }))
            .ToList();

        rows.Should().ContainSingle();

        // Because FakeXeroApiClient returns an invoice, check the serialized payload
        using var doc = JsonDocument.Parse(rows[0].Payload);
        doc.RootElement.GetProperty("InvoiceID").GetString().Should().Be("abc-123");
        doc.RootElement.GetProperty("Status").GetString().Should().Be("PAID");
    }

    [Test]
    public async Task XeroWebhook_Post_InvalidSignature_Returns401()
    {
        var payload = JsonSerializer.Serialize(new
        {
            events = new[]
            {
                new { resourceId = "xyz-999", eventCategory = "INVOICE", eventType = "CREATE" }
            }
        });

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/xero")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-xero-signature", "bad-sig");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string ComputeHmacSha256Base64(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hash);
    }
}

/// <summary>
/// Fake implementation for tests — avoids live Xero calls
/// </summary>
public class FakeXeroApiClient : IXeroApiClient
{
    public Task<XeroInvoice?> GetInvoiceByIdAsync(string invoiceId)
    {
        return Task.FromResult<XeroInvoice?>(new XeroInvoice
        {
            InvoiceID = invoiceId,
            Type = "ACCREC",
            Status = "PAID",
            Reference = "TEST"
        });
    }

    public Task<IReadOnlyList<string>> FetchInvoicesAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}
