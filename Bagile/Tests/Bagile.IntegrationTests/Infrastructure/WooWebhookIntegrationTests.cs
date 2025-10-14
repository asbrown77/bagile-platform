using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

[TestFixture]
[Category("Integration")]
public class WooWebhookIntegrationTests
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

        // The schema is already applied by DatabaseFixture.ApplySchemaAsync()
        await _db.ExecuteAsync("DELETE FROM bagile.raw_orders;");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((ctx, config) =>
                {
                    var dict = new Dictionary<string, string>
                    {
                        ["ConnectionStrings:DefaultConnection"] = connStr,
                        ["WooCommerce:WebhookSecret"] = _webhookSecret
                    };
                    config.AddInMemoryCollection(dict);
                });

                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(IXeroApiClient));
                    if (descriptor != null)
                        services.Remove(descriptor);

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
    public async Task Minimal_Post_Works()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/health", new StringContent("{}", Encoding.UTF8, "application/json"));
        Console.WriteLine(response.StatusCode);
    }


    [Test]
    public async Task Webhook_Post_ValidPayload_UpsertsRawOrder()
    {
        // Arrange
        var payload = "{\"id\":123,\"foo\":\"bar\"}";
        var signature = ComputeHmacSha256Base64(payload, _webhookSecret);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/woo")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-WC-Webhook-Signature", signature);

        Console.WriteLine($"Request content: {await request.Content.ReadAsStringAsync()}");

        // Act
        var response = await _client.SendAsync(request);

        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {response.StatusCode}\n{body}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);


        var rows = (await _db.QueryAsync<RawOrder>(
            "SELECT * FROM bagile.raw_orders WHERE external_id = @id", new { id = "123" }))
            .ToList();

        rows.Should().ContainSingle();
        using var doc = JsonDocument.Parse(rows[0].Payload);
        doc.RootElement.GetProperty("foo").GetString().Should().Be("bar");
    }

    [Test]
    public async Task Webhook_Post_InvalidSignature_Returns401()
    {
        var payload = "{\"id\":456}";
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/woo")
        {
            Content = content
        };
        // intentionally wrong signature
        request.Headers.Add("X-WC-Webhook-Signature", "invalidsig");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string ComputeHmacSha256Base64(string payload, string secret)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(payload); // always the same bytes the handler sees
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(bodyBytes);
        return Convert.ToBase64String(hash);
    }

}
