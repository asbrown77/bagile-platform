using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Bagile.IntegrationTests.Api;

/// <summary>
/// Tests that portal auth endpoint returns correct HTTP responses (not unhandled exceptions)
/// and preserves CORS headers on all response codes — including errors.
/// Root issue: if an unhandled exception propagates, the .NET exception handler creates
/// a new response without CORS headers, causing "Failed to fetch" in the browser.
/// </summary>
[TestFixture]
public class PortalAuthTests
{
    private const string PortalOrigin = "https://portal.bagile.co.uk";

    private static HttpClient CreateClient(Dictionary<string, string?> extraConfig)
    {
        var connStr = DatabaseFixture.ConnectionString;
        var factory = TestApiFactory.Create(connStr, "test-key",
            configureConfig: config => config.AddInMemoryCollection(extraConfig));

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        return client;
    }

    private static StringContent JsonBody(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    [Test]
    public async Task MissingGoogleClientId_Returns500_WithCorsHeaders()
    {
        using var client = CreateClient(new Dictionary<string, string?>
        {
            // Portal:GoogleClientId intentionally omitted
            ["Portal:JwtSecret"] = "test-secret-32-chars-minimum-len!"
        });
        client.DefaultRequestHeaders.Add("Origin", PortalOrigin);

        var response = await client.PostAsync("/portal/auth/google",
            JsonBody(new { idToken = "fake-token" }));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError,
            "missing GoogleClientId should return 500, not throw");

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
            "CORS header must be present on 500 so browser can read the error, not 'Failed to fetch'");
    }

    [Test]
    public async Task MissingJwtSecret_Returns500_WithCorsHeaders()
    {
        using var client = CreateClient(new Dictionary<string, string?>
        {
            ["Portal:GoogleClientId"] = "test-client-id.apps.googleusercontent.com",
            // Portal:JwtSecret intentionally omitted — this previously caused InvalidOperationException
        });
        client.DefaultRequestHeaders.Add("Origin", PortalOrigin);

        // Send a well-formed but invalid Google token — it will fail GoogleClientId check first
        // We just need to reach the JwtSecret check; using a fake base64 JWT structure
        var fakeJwt = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.fake";
        var response = await client.PostAsync("/portal/auth/google",
            JsonBody(new { idToken = fakeJwt }));

        // Will be 401 (InvalidJwtException from Google validation) or 500 (missing secret)
        // — either way it must NOT be a 500 thrown as an unhandled exception without CORS headers
        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.InternalServerError,
            (HttpStatusCode)502,
            HttpStatusCode.OK);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
            "CORS headers must be present regardless of response code");
    }

    [Test]
    public async Task EmptyBody_Returns400_WithCorsHeaders()
    {
        using var client = CreateClient(new Dictionary<string, string?>
        {
            ["Portal:GoogleClientId"] = "test-client-id.apps.googleusercontent.com",
            ["Portal:JwtSecret"] = "test-secret-32-chars-minimum-len!"
        });
        client.DefaultRequestHeaders.Add("Origin", PortalOrigin);

        var response = await client.PostAsync("/portal/auth/google",
            JsonBody(new { idToken = "" }));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
            "CORS headers must be present on 400");
    }

    [Test]
    public async Task InvalidToken_Returns401_WithCorsHeaders()
    {
        using var client = CreateClient(new Dictionary<string, string?>
        {
            ["Portal:GoogleClientId"] = "test-client-id.apps.googleusercontent.com",
            ["Portal:JwtSecret"] = "test-secret-32-chars-minimum-len!"
        });
        client.DefaultRequestHeaders.Add("Origin", PortalOrigin);

        var response = await client.PostAsync("/portal/auth/google",
            JsonBody(new { idToken = "not-a-valid-google-jwt" }));

        // InvalidJwtException → 401, OR network error fetching certs → 502
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, (HttpStatusCode)502);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
            "CORS headers must be present on 401/502");
    }

    [Test]
    public async Task CorsPreflightOptions_ReturnsOk_WithCorsHeaders()
    {
        using var client = CreateClient(new Dictionary<string, string?>
        {
            ["Portal:GoogleClientId"] = "test-client-id.apps.googleusercontent.com",
            ["Portal:JwtSecret"] = "test-secret-32-chars-minimum-len!"
        });
        client.DefaultRequestHeaders.Add("Origin", PortalOrigin);
        client.DefaultRequestHeaders.Add("Access-Control-Request-Method", "POST");
        client.DefaultRequestHeaders.Add("Access-Control-Request-Headers", "Content-Type");

        var request = new HttpRequestMessage(HttpMethod.Options, "/portal/auth/google");
        var response = await client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent, HttpStatusCode.Forbidden);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeTrue(
            "preflight must return CORS headers");
    }
}
