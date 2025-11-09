using Bagile.Domain.Entities;
using Dapper;
using FluentAssertions;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Bagile.IntegrationTests.Infrastructure;

[TestFixture]
[Category("Integration")]
public class WooWebhookIntegrationTests : IntegrationTestBase
{
    private readonly string _webhookSecret = "testsecret";

    [Test]
    public async Task Minimal_Post_Works()
    {
        var response = await _client.PostAsync(
            "/health",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Console.WriteLine(response.StatusCode);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
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

        // Act
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response: {response.StatusCode}\n{body}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var rows = (await _db.QueryAsync<RawOrder>(
                "SELECT * FROM bagile.raw_orders WHERE external_id = @id",
                new { id = "123" }))
            .ToList();

        rows.Should().ContainSingle();

        using var doc = JsonDocument.Parse(rows[0].Payload);
        doc.RootElement.GetProperty("foo").GetString().Should().Be("bar");
    }

    [Test]
    public async Task Webhook_Post_InvalidSignature_Returns401()
    {
        var payload = "{\"id\":456}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/woo")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        // intentionally wrong signature
        request.Headers.Add("X-WC-Webhook-Signature", "invalidsig");

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string ComputeHmacSha256Base64(string payload, string secret)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(payload);
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(bodyBytes);
        return Convert.ToBase64String(hash);
    }
}
