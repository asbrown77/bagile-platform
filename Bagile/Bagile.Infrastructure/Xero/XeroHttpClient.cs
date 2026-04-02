using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Bagile.Infrastructure.Xero;

/// <summary>
/// Generic Xero API HTTP client for the API layer.
///
/// Auth flow:
///   1. Obtain a valid access token from IXeroTokenManager (refreshes if needed).
///   2. Set Authorization + xero-tenant-id + Accept headers.
///   3. On 401 response: the token may have been rotated by the ETL between our
///      cache check and the actual HTTP call. Force a fresh token and retry once.
///   4. Throw on any other non-success status.
///
/// Registered as a typed HttpClient: AddHttpClient&lt;IXeroHttpClient, XeroHttpClient&gt;()
/// </summary>
public class XeroHttpClient : IXeroHttpClient
{
    private readonly IXeroTokenManager _tokenManager;
    private readonly HttpClient _http;
    private readonly ILogger<XeroHttpClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private const string BaseUrl = "https://api.xero.com";

    public XeroHttpClient(
        HttpClient http,
        IXeroTokenManager tokenManager,
        ILogger<XeroHttpClient> logger)
    {
        _http = http;
        _tokenManager = tokenManager;
        _logger = logger;
    }

    public async Task<T> GetAsync<T>(string path, CancellationToken ct = default)
    {
        var response = await SendWithRetryAsync(HttpMethod.Get, path, body: null, ct);
        return await DeserialiseAsync<T>(response, path, ct);
    }

    public async Task<T> PostAsync<T>(string path, object body, CancellationToken ct = default)
    {
        var response = await SendWithRetryAsync(HttpMethod.Post, path, body, ct);
        return await DeserialiseAsync<T>(response, path, ct);
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var token = await _tokenManager.GetAccessTokenAsync(ct);
        var response = await SendAsync(method, path, body, token, ct);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Token may have been externally rotated (e.g. ETL ran a refresh between our
            // cache read and this HTTP call). Force another refresh and retry once.
            _logger.LogWarning(
                "Xero returned 401 for {Method} {Path}. Retrying once with a fresh token.",
                method, path);

            // Invalidate in-memory cache by clearing expiry so next call forces refresh
            token = await _tokenManager.GetAccessTokenAsync(ct);
            response = await SendAsync(method, path, body, token, ct);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Xero API error. Method: {Method}, Path: {Path}, Status: {Status}, Body: {Body}",
                method, path, response.StatusCode, errorBody);

            response.EnsureSuccessStatusCode(); // throws HttpRequestException with status code
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpMethod method, string path, object? body, string accessToken, CancellationToken ct)
    {
        var url = $"{BaseUrl}{path}";
        var req = new HttpRequestMessage(method, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.Add("xero-tenant-id", _tokenManager.TenantId);
        req.Headers.Add("Accept", "application/json");

        if (body is not null)
        {
            var json = JsonSerializer.Serialize(body);
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return await _http.SendAsync(req, ct);
    }

    private static async Task<T> DeserialiseAsync<T>(
        HttpResponseMessage response, string path, CancellationToken ct)
    {
        var json = await response.Content.ReadAsStringAsync(ct);

        return JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new InvalidOperationException(
                $"Xero API returned null or empty response for path '{path}'.");
    }
}
