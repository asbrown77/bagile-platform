using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;

namespace Bagile.IntegrationTests.Api;

[TestFixture]
public class ApiKeyAuthTests : IntegrationTestBase
{
    [Test]
    public async Task Protected_Endpoints_Should_Return_401_Without_ApiKey()
    {
        var protectedUrls = new[]
        {
            "/api/course-schedules?page=1&pageSize=1",
            "/api/enrolments?page=1&pageSize=1",
        };

        using var client = _factory.CreateClient();

        foreach (var url in protectedUrls)
        {
            var response = await client.GetAsync(url);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                because: $"endpoint {url} should be protected by API key");
        }
    }

    [Test]
    public async Task Public_Endpoints_Should_Not_Require_ApiKey()
    {
        var publicUrls = new[]
        {
            "/",
            "/health",
            "/swagger/index.html",
            "/xero/connect"
        };

        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        foreach (var url in publicUrls)
        {
            var response = await client.GetAsync(url);
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect);
        }
    }
}