using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;

namespace Bagile.Api.Services;

public class XeroAuthSetupService
{
    private readonly IConfiguration _config;
    private readonly ILogger<XeroAuthSetupService> _logger;
    private readonly HttpClient _http;

    public XeroAuthSetupService(IConfiguration config, ILogger<XeroAuthSetupService> logger, HttpClient http)
    {
        _config = config;
        _logger = logger;
        _http = http;
    }

    public async Task<(string accessToken, string refreshToken, int expiresIn)> ExchangeCodeForTokensAsync(string code)
    {
        var clientId = _config["Xero:ClientId"]!;
        var clientSecret = _config["Xero:ClientSecret"]!;
        var redirectUri = _config["Xero:RedirectUri"]!;

        var creds = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        var request = new HttpRequestMessage(HttpMethod.Post, "https://identity.xero.com/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", creds);

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Token exchange response: {Body}", body);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to exchange code: {response.StatusCode} - {body}");

        using var doc = JsonDocument.Parse(body);
        return (
            doc.RootElement.GetProperty("access_token").GetString()!,
            doc.RootElement.GetProperty("refresh_token").GetString()!,
            doc.RootElement.GetProperty("expires_in").GetInt32()
        );
    }

    public async Task<(string tenantId, string tenantName)> GetTenantAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.xero.com/connections");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        _logger.LogInformation("Connections response: {Body}", body);

        var tenants = JsonDocument.Parse(body).RootElement.EnumerateArray().ToList();
        if (tenants.Count == 0)
            throw new InvalidOperationException("No Xero tenants found for this account.");

        JsonElement selected = default;
        foreach (var tenant in tenants)
        {
            var name = tenant.GetProperty("tenantName").GetString();
            if (!string.IsNullOrEmpty(name) &&
                name.Contains("b-agile", StringComparison.OrdinalIgnoreCase))
            {
                selected = tenant;
                break;
            }
        }

        // fallback if none matched “b-agile”
        if (selected.ValueKind == JsonValueKind.Undefined)
            selected = tenants.First();

        return (
            selected.GetProperty("tenantId").GetString()!,
            selected.GetProperty("tenantName").GetString()!
        );
    }


    public async Task SaveIntegrationTokenAsync(string refresh, string access, int expiresIn, string tenantId)
    {
        var connStr = _config.GetConnectionString("DefaultConnection")!;
        await using var db = new NpgsqlConnection(connStr);

        await db.ExecuteAsync(@"
            INSERT INTO bagile.integration_tokens (source, refresh_token, access_token, expires_at, tenant_id)
            VALUES ('xero', @r, @a, @e, @t)
            ON CONFLICT (source)
            DO UPDATE SET refresh_token=@r, access_token=@a, expires_at=@e, tenant_id=@t",
            new
            {
                r = refresh,
                a = access,
                e = DateTime.UtcNow.AddSeconds(expiresIn),
                t = tenantId
            });

        _logger.LogInformation("Integration token saved for tenant {TenantId}", tenantId);
    }
}
