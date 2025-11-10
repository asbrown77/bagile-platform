using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bagile.Domain.Entities;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Dapper;
using FluentAssertions;

namespace Bagile.IntegrationTests;

[TestFixture]
[Category("Integration")]
public class XeroWebhookIntegrationTests : IntegrationTestBase
{
    private string _webhookSecret = "testsecret";

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

    public Task<IEnumerable<string>> FetchInvoicesAsync(DateTime? modifiedSince = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetInvoiceByUrlAsync(string resourceUrl, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}