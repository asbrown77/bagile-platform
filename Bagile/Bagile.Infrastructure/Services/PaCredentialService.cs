using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Bagile.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bagile.Infrastructure.Services;

/// <summary>
/// HTTP client for trainer credential management via the bagile-pa service.
/// Handles scrumorg_username, scrumorg_password, and session cookie refresh.
/// </summary>
public class PaCredentialService : IPaCredentialService
{
    // Playwright login can take up to 2 minutes; 5-second connect timeout for fast failure on everything else.
    // ConnectCallback forces IPv4 resolution — Docker DNS can return IPv6 for container hostnames and .NET's
    // Happy Eyeballs algorithm tries IPv6 first, which fails when the PA service binds to 0.0.0.0 only.
    private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        ConnectTimeout = TimeSpan.FromSeconds(5),
        ConnectCallback = async (ctx, ct) =>
        {
            var entry = await Dns.GetHostEntryAsync(ctx.DnsEndPoint.Host, AddressFamily.InterNetwork, ct);
            var ep = new IPEndPoint(entry.AddressList[0], ctx.DnsEndPoint.Port);
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                { NoDelay = true };
            await socket.ConnectAsync(ep, ct);
            return new NetworkStream(socket, ownsSocket: true);
        },
    })
    { Timeout = TimeSpan.FromMinutes(10) };

    private readonly ILogger<PaCredentialService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public PaCredentialService(
        IConfiguration config,
        ILogger<PaCredentialService> logger)
    {
        _logger = logger;
        _baseUrl = (config["PaService:BaseUrl"] ?? "http://bagile-pa:3001").TrimEnd('/');
        _apiKey = config["PaService:ApiKey"] ?? "";
    }

    public async Task<TrainerCredentialStatus> GetTrainerScrumOrgStatusAsync(
        int trainerId,
        CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/trainer-credentials/trainer-{trainerId}";
        using var request = BuildRequest(HttpMethod.Get, url);

        var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, body, "GetTrainerScrumOrgStatus");

        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var keys = root.TryGetProperty("keys", out var keysEl)
            ? keysEl.EnumerateArray().Select(k => k.GetString() ?? "").ToList()
            : new List<string>();

        var username = root.TryGetProperty("username", out var unEl) && unEl.ValueKind != JsonValueKind.Null
            ? unEl.GetString()
            : null;

        return new TrainerCredentialStatus(
            Username: username,
            HasPassword: keys.Contains("scrumorg_password"),
            HasCookies: keys.Contains("scrumorg_session_cookies")
        );
    }

    public async Task SetTrainerScrumOrgCredentialAsync(
        int trainerId,
        string key,
        string value,
        CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/trainer-credentials/trainer-{trainerId}/{key}";
        var payload = JsonSerializer.Serialize(new { value });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var request = BuildRequest(HttpMethod.Put, url, content);

        var response = await _httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        EnsureSuccess(response, body, "SetTrainerScrumOrgCredential");
    }

    public async Task<ScrumOrgLoginResult> RefreshTrainerSessionAsync(
        int trainerId,
        CancellationToken ct = default)
    {
        _logger.LogInformation("Refreshing Scrum.org session for trainer-{TrainerId}", trainerId);

        var url = $"{_baseUrl}/playwright/scrumorg-login";
        var payload = JsonSerializer.Serialize(new { trainerId = $"trainer-{trainerId}" });
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var request = BuildRequest(HttpMethod.Post, url, content);

        try
        {
            var response = await _httpClient.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var success = root.TryGetProperty("success", out var s) && s.GetBoolean();
            var errorMessage = root.TryGetProperty("errorMessage", out var e) && e.ValueKind != JsonValueKind.Null
                ? e.GetString()
                : null;

            if (success)
                _logger.LogInformation("Scrum.org session refreshed for trainer-{TrainerId}", trainerId);
            else
                _logger.LogWarning("Scrum.org session refresh failed for trainer-{TrainerId}: {Error}", trainerId, errorMessage);

            return new ScrumOrgLoginResult(success, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PA service for Scrum.org login (trainer-{TrainerId})", trainerId);
            return new ScrumOrgLoginResult(false, $"Failed to reach PA service: {ex.Message}");
        }
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        var request = new HttpRequestMessage(method, url) { Content = content };
        if (!string.IsNullOrWhiteSpace(_apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        return request;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string body, string operation)
    {
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"{operation}: PA service returned HTTP {(int)response.StatusCode}: {body}");
    }
}
