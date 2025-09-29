using Bagile.Infrastructure;
using Dapper;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Npgsql;
using NUnit.Framework;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Bagile.IntegrationTests
{
    [TestFixture]
    [Category("Integration")]
    public class WebhookRawOrderTests
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;
        private string _webhookSecret = "testsecret";
        private NpgsqlConnection _db;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // Use your test DB connection string here
            var connStr = DatabaseFixture.ConnectionString;
            _db = new NpgsqlConnection(connStr);
            await _db.OpenAsync();

            // Clean up before test
            await _db.ExecuteAsync("DELETE FROM bagile.raw_orders;");

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((ctx, config) =>
                    {
                        var dict = new Dictionary<string, string>
                        {
                            ["ConnectionStrings:DefaultConnection"] = connStr,
                            ["WooCommerce:WebhookSecret"] = _webhookSecret
                        };
                        config.AddInMemoryCollection(dict);
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
        public async Task Webhook_Post_ValidPayload_UpsertsRawOrder()
        {
            // Arrange
            var payload = "{\"id\":123,\"foo\":\"bar\"}";
            var signature = ComputeHmacSha256Base64(payload, _webhookSecret);

            var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/woo")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-WC-Webhook-Signature", signature);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var rows = (await _db.QueryAsync<RawOrder>(
                "SELECT * FROM bagile.raw_orders WHERE external_id = @id", new { id = "123" })).ToList();

            rows.Should().ContainSingle();
            rows[0].Payload.Should().Contain("\"foo\":\"bar\"");
        }

        [Test]
        public async Task Webhook_Post_InvalidSignature_Returns401()
        {
            var payload = "{\"id\":456}";
            var request = new HttpRequestMessage(HttpMethod.Post, "/webhooks/woo")
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-WC-Webhook-Signature", "invalidsig");

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static string ComputeHmacSha256Base64(string payload, string secret)
        {
            using var hmac = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }
}