using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bagile.Infrastructure.Clients;

public class XeroAuthService
{
    private readonly IConfiguration _config;
    private readonly ILogger<XeroAuthService> _logger;
    private readonly HttpClient _http;

    private const string TokenFile = "xero_token.json";

    public XeroAuthService(HttpClient http, IConfiguration config, ILogger<XeroAuthService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<string> EnsureAccessTokenAsync()
    {
        _logger.LogInformation("Refreshing Xero access token...");

        // Prefer the persisted refresh token if available
        string refreshToken;
        if (File.Exists(TokenFile))
        {
            var saved = JsonDocument.Parse(await File.ReadAllTextAsync(TokenFile));
            refreshToken = saved.RootElement.GetProperty("refresh_token").GetString()!;
        }
        else
        {
            refreshToken = _config["Xero:RefreshToken"]!;
        }

        var clientId = _config["Xero:ClientId"]!;
        var clientSecret = _config["Xero:ClientSecret"]!;

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://identity.xero.com/connect/token")
        {
            Content = new FormUrlEncodedContent(form)
        };

        // Proper Basic auth header
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Xero token refresh failed ({Status}): {Body}", response.StatusCode, body);
            response.EnsureSuccessStatusCode(); // throws
        }

        using var doc = JsonDocument.Parse(body);
        var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
        var newRefreshToken = doc.RootElement.GetProperty("refresh_token").GetString()!;
        var expiresIn = doc.RootElement.GetProperty("expires_in").GetInt32();

        // Save only what we need
        var save = new
        {
            access_token = accessToken,
            refresh_token = newRefreshToken,
            expires_at = DateTime.UtcNow.AddSeconds(expiresIn)
        };
        await File.WriteAllTextAsync(TokenFile, JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true }));

        _logger.LogInformation("Xero token refreshed successfully. Expires in {Minutes} minutes", expiresIn / 60);

        return accessToken;
    }
}
