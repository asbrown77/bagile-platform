using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Filters;
using Bagile.Infrastructure.Models;
using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

public class XeroApiClient : IXeroApiClient
{
    private readonly IConfiguration _config;
    private readonly ILogger<XeroApiClient> _logger;
    private readonly HttpClient _http;
    private readonly XeroTokenRefreshService _auth;

    public XeroApiClient(HttpClient http, IConfiguration config, ILogger<XeroApiClient> logger, XeroTokenRefreshService auth)
    {
        _http = http;
        _config = config;
        _logger = logger;
        _auth = auth;
    }

    // 1️⃣ For webhooks
    public async Task<XeroInvoice?> GetInvoiceByIdAsync(string invoiceId)
    {
        var url = $"https://api.xero.com/api.xro/2.0/Invoices/{invoiceId}";
        var response = await SendAsync(url);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var invoice = doc.RootElement.GetProperty("Invoices").EnumerateArray().FirstOrDefault();
        return JsonSerializer.Deserialize<XeroInvoice>(invoice.GetRawText());
    }

    public async Task<IEnumerable<string>> FetchInvoicesAsync(
        DateTime? modifiedSince = null, CancellationToken ct = default)
    {
        var url = "https://api.xero.com/api.xro/2.0/Invoices";
        var where = XeroInvoiceFilter.ToXeroWhereClause(modifiedSince);
        url += $"?where={Uri.EscapeDataString(where)}";

        _logger.LogInformation("Xero API query: {Url}", url);

        var response = await SendAsync(url, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("Invoices")
                 .EnumerateArray()
                 .Select(x => x.GetRawText())
                 .ToList();
    }

    private async Task<HttpResponseMessage> SendAsync(string url, CancellationToken ct = default)
    {
        var token = await _auth.EnsureAccessTokenAsync();
        var tenantId = await EnsureTenantIdAsync(token, ct);

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("xero-tenant-id", tenantId);

        var res = await _http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();
        return res;
    }

    private async Task<string> EnsureTenantIdAsync(string accessToken, CancellationToken ct)
    {
        // check if we already have one cached in DB
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
