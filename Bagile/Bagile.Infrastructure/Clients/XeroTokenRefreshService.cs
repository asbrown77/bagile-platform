using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Bagile.Infrastructure.Clients;

public class XeroTokenRefreshService
{
    private readonly IConfiguration _config;
    private readonly ILogger<XeroTokenRefreshService> _logger;
    private readonly HttpClient _http;
    private readonly NpgsqlConnection _db;

    public XeroTokenRefreshService(HttpClient http, IConfiguration config, ILogger<XeroTokenRefreshService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;

        var connStr = _config.GetConnectionString("DefaultConnection")!;
        _db = new NpgsqlConnection(connStr);
    }

    public async Task<string> EnsureAccessTokenAsync()
    {
        _logger.LogInformation("Checking Xero token status...");

        var token = await _db.QuerySingleOrDefaultAsync<(string Refresh, string? Access, DateTime? Expires)>(
            "SELECT refresh_token AS Refresh, access_token AS Access, expires_at AS Expires FROM bagile.integration_tokens WHERE source='xero'");

        if (token.Refresh == null)
        {
            _logger.LogError("No Xero refresh token found. Run https://api.bagile.co.uk/xero/connect first.");
            throw new InvalidOperationException("Xero integration not initialised.");
        }

        if (token.Access != null && token.Expires.HasValue && token.Expires > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogInformation("Existing access token still valid.");
            return token.Access;
        }

        _logger.LogInformation("Refreshing Xero token...");

        var clientId = _config["Xero:ClientId"]!;
        var clientSecret = _config["Xero:ClientSecret"]!;
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = token.Refresh
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://identity.xero.com/connect/token")
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")));

        var res = await _http.SendAsync(req);
        var body = await res.Content.ReadAsStringAsync();

        _logger.LogInformation("Xero response ({Status}): {Body}", res.StatusCode, body);

        res.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var access = doc.RootElement.GetProperty("access_token").GetString()!;
        var refresh = doc.RootElement.GetProperty("refresh_token").GetString()!;
        var expires = DateTime.UtcNow.AddSeconds(doc.RootElement.GetProperty("expires_in").GetInt32());

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.integration_tokens (source, refresh_token, access_token, expires_at)
            VALUES ('xero', @r, @a, @e)
            ON CONFLICT (source)
            DO UPDATE SET refresh_token=@r, access_token=@a, expires_at=@e",
            new { r = refresh, a = access, e = expires });

        _logger.LogInformation("Xero token refreshed successfully until {Expires}", expires);
        return access;
    }
}
