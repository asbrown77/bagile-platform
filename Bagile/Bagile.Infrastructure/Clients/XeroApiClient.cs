using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Filters;
using Bagile.Infrastructure.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

public class XeroApiClient : IXeroApiClient
{
    private readonly IConfiguration _config;
    private readonly ILogger<XeroApiClient> _logger;
    private readonly HttpClient _http;
    private readonly XeroTokenRefreshService _auth;

    private static readonly SemaphoreSlim _rateLock = new(1, 1);
    private DateTime _lastCall = DateTime.MinValue;
    private static readonly TimeSpan MinGap = TimeSpan.FromMilliseconds(250); // 4 calls per second

    public XeroApiClient(HttpClient http, IConfiguration config, ILogger<XeroApiClient> logger, XeroTokenRefreshService auth)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _auth = auth;
    }

    // 1️⃣ For webhooks by invoice id
    public async Task<XeroInvoice?> GetInvoiceByIdAsync(string invoiceId)
    {
        var url = $"https://api.xero.com/api.xro/2.0/Invoices/{invoiceId}";
        var response = await SendAsync(url);
        var json = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("Invoices").EnumerateArray().FirstOrDefault();
        if (invoice.ValueKind == JsonValueKind.Undefined)
            return null;

        return JsonSerializer.Deserialize<XeroInvoice>(invoice.GetRawText());
    }

    // 2️⃣ For ETL collector
    public async Task<IEnumerable<string>> FetchInvoicesAsync(
        DateTime? modifiedSince = null, CancellationToken ct = default)
    {
        var url = "https://api.xero.com/api.xro/2.0/Invoices";
        var where = XeroInvoiceFilter.ToXeroWhereClause(modifiedSince);
        url += $"?where={Uri.EscapeDataString(where)}";

        _logger.LogInformation("Xero API query: {Url}", url);

        var response = await SendAsync(url, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("Invoices")
                 .EnumerateArray()
                 .Select(x => x.GetRawText())
                 .ToList();
    }

    // 3️⃣ Used by RawOrderTransformer to enrich webhook envelopes
    public async Task<string> GetInvoiceByUrlAsync(string resourceUrl, CancellationToken ct = default)
    {
        var response = await SendAsync(resourceUrl, ct);
        return await response.Content.ReadAsStringAsync(ct);
    }

    // ---------------------------------------------------------
    // Core HTTP sender with rate limiting and 429 handling
    // ---------------------------------------------------------
    private async Task<HttpResponseMessage> SendAsync(string url, CancellationToken ct = default)
    {
        var token = await _auth.EnsureAccessTokenAsync();
        var tenantId = await EnsureTenantIdAsync(token, ct);

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("xero-tenant-id", tenantId);

        await _rateLock.WaitAsync(ct);
        try
        {
            // Throttle slightly to avoid hitting Xero rate limits
            var elapsed = DateTime.UtcNow - _lastCall;
            if (elapsed < MinGap)
                await Task.Delay(MinGap - elapsed, ct);

            var res = await _http.SendAsync(req, ct);
            _lastCall = DateTime.UtcNow;

            // Handle 429 Too Many Requests
            if (res.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);
                _logger.LogWarning(
                    "Xero rate limit hit for {Url}. Status 429. Retry after {Seconds} seconds.",
                    url,
                    retryAfter.TotalSeconds);

                throw new XeroRateLimitException("Xero rate limit exceeded", retryAfter);
            }

            res.EnsureSuccessStatusCode();
            return res;
        }
        finally
        {
            _rateLock.Release();
        }
    }

    private async Task<string> EnsureTenantIdAsync(string accessToken, CancellationToken ct)
    {
        var connStr = _config.GetConnectionString("DefaultConnection");
        await using var db = new Npgsql.NpgsqlConnection(connStr);

        var tenantId = await db.ExecuteScalarAsync<string>(
            "SELECT tenant_id FROM bagile.integration_tokens WHERE source='xero'");

        if (!string.IsNullOrEmpty(tenantId))
            return tenantId;

        _logger.LogInformation("Discovering Xero tenant ID...");

        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.xero.com/connections");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var res = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        res.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(body);
        var tenant = doc.RootElement.EnumerateArray().FirstOrDefault();
        var discoveredTenantId = tenant.GetProperty("tenantId").GetString()!;

        await db.ExecuteAsync(@"
            UPDATE bagile.integration_tokens
            SET tenant_id = @t
            WHERE source = 'xero'", new { t = discoveredTenantId });

        _logger.LogInformation("Xero tenant discovered and cached: {TenantId}", discoveredTenantId);
        return discoveredTenantId;
    }
}

// ---------------------------------------------------------
// Supporting class for 429 handling
// ---------------------------------------------------------
public class XeroRateLimitException : Exception
{
    public TimeSpan? RetryAfter { get; }

    public XeroRateLimitException(string message, TimeSpan? retryAfter = null)
        : base(message)
    {
        RetryAfter = retryAfter;
    }
}
