using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Bagile.Infrastructure.Xero;

/// <summary>
/// Thread-safe Xero OAuth 2.0 token manager for the API layer.
///
/// Lifecycle:
///   1. GetAccessTokenAsync() checks the in-memory cached token first (avoids DB hit on every call).
///   2. If expired (or within 5 min of expiry), acquires SemaphoreSlim to prevent concurrent refreshes.
///   3. Re-checks after acquiring the lock — another thread may have refreshed while waiting.
///   4. Calls Xero token endpoint, saves new tokens to DB and updates in-memory cache.
///
/// Storage: bagile.integration_tokens (source = 'xero') — shared with ETL's XeroTokenRefreshService.
/// Both services write to the same row, which is correct — the most recently refreshed token wins.
/// Xero refresh tokens are rotated on every refresh. If both the API and ETL refresh concurrently,
/// one will produce a stale refresh token. In practice, the API and ETL should not refresh
/// simultaneously — the 5-minute expiry buffer makes this collision window very small.
///
/// Registered as a typed HttpClient: AddHttpClient&lt;XeroTokenManager&gt;()
/// </summary>
public class XeroTokenManager : IXeroTokenManager
{
    private readonly XeroTokenRepository _repository;
    private readonly HttpClient _http;
    private readonly ILogger<XeroTokenManager> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;

    // In-memory cache — avoids a DB round-trip on every API call
    private string? _cachedAccessToken;
    private DateTime _cachedExpiresAt = DateTime.MinValue;

    // One refresh at a time — prevents duplicate refresh races under concurrent load
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(5);

    public string TenantId { get; }

    public XeroTokenManager(
        HttpClient http,
        XeroTokenRepository repository,
        IConfiguration config,
        ILogger<XeroTokenManager> logger)
    {
        _http = http;
        _repository = repository;
        _logger = logger;

        _clientId = config["Xero:ClientId"]
            ?? throw new InvalidOperationException("Xero:ClientId not configured.");
        _clientSecret = config["Xero:ClientSecret"]
            ?? throw new InvalidOperationException("Xero:ClientSecret not configured.");
        TenantId = config["Xero:TenantId"] ?? "aef46d85-ec9c-475b-990d-5480d708605c";
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        // Fast path: in-memory token still valid — no lock, no DB
        if (IsTokenValid(_cachedAccessToken, _cachedExpiresAt))
            return _cachedAccessToken!;

        // Slow path: acquire lock to serialise refreshes
        await _refreshLock.WaitAsync(ct);
        try
        {
            // Re-check after acquiring lock — another waiter may have refreshed already
            if (IsTokenValid(_cachedAccessToken, _cachedExpiresAt))
                return _cachedAccessToken!;

            return await RefreshAsync(ct);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string> RefreshAsync(CancellationToken ct)
    {
        var row = await _repository.GetAsync(ct);

        if (row is null || string.IsNullOrWhiteSpace(row.RefreshToken))
        {
            throw new InvalidOperationException(
                "Xero integration not initialised. Complete the OAuth flow at GET /xero/connect " +
                "or ensure the bagile.integration_tokens row exists with a valid refresh_token.");
        }

        // If the DB token is still valid (e.g. refreshed by ETL recently), adopt it
        if (IsTokenValid(row.AccessToken, row.ExpiresAt))
        {
            _cachedAccessToken = row.AccessToken;
            _cachedExpiresAt = row.ExpiresAt!.Value;
            _logger.LogDebug("Adopted valid token from DB cache. Expires {ExpiresAt} UTC.", _cachedExpiresAt);
            return _cachedAccessToken!;
        }

        _logger.LogInformation("Refreshing Xero access token...");

        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = row.RefreshToken,
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "https://identity.xero.com/connect/token")
        {
            Content = new FormUrlEncodedContent(form),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}")));

        var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Xero token refresh failed. Status: {Status}. Body: {Body}",
                res.StatusCode, body);

            // 400 with "invalid_grant" means the refresh token has expired (~60 days idle)
            // or was rotated by a concurrent refresh from another process (e.g. ETL).
            throw new InvalidOperationException(
                $"Xero token refresh failed ({res.StatusCode}). " +
                "The refresh token may have expired or been rotated by another service. " +
                "Re-authorise at GET /xero/connect to obtain fresh tokens.");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var newAccessToken = root.GetProperty("access_token").GetString()!;
        var newRefreshToken = root.GetProperty("refresh_token").GetString()!;
        var expiresIn = root.GetProperty("expires_in").GetInt32();
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);

        await _repository.SaveAsync(newAccessToken, newRefreshToken, expiresAt, ct);

        _cachedAccessToken = newAccessToken;
        _cachedExpiresAt = expiresAt;

        _logger.LogInformation(
            "Xero token refreshed successfully. Expires at {ExpiresAt} UTC.",
            expiresAt.ToString("O"));

        return _cachedAccessToken;
    }

    private static bool IsTokenValid(string? token, DateTime? expiresAt)
        => !string.IsNullOrWhiteSpace(token)
            && expiresAt.HasValue
            && expiresAt.Value > DateTime.UtcNow.Add(ExpiryBuffer);
}
