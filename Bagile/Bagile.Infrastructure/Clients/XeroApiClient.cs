using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

public class XeroApiClient : IXeroApiClient
{
    private readonly IConfiguration _config;
    private readonly ILogger<XeroApiClient> _logger;
    private readonly HttpClient _http;

    public XeroApiClient(HttpClient http, IConfiguration config, ILogger<XeroApiClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
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

    public Task<IReadOnlyList<string>> FetchInvoicesAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    // 2️⃣ For ETL
    public async Task<IEnumerable<string>> FetchInvoicesAsync(DateTime? modifiedSince = null, CancellationToken ct = default)
    {
        var url = "https://api.xero.com/api.xro/2.0/Invoices";
        if (modifiedSince != null)
            url += $"?where=Date>={modifiedSince:yyyy-MM-dd}";

        var response = await SendAsync(url);
        var json = await response.Content.ReadAsStringAsync(ct);

        var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("Invoices")
                 .EnumerateArray()
                 .Select(x => x.GetRawText())
                 .ToList();
    }

    private async Task<HttpResponseMessage> SendAsync(string url)
    {
        var token = _config["Xero:AccessToken"];
        var tenant = _config["Xero:TenantId"];
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("xero-tenant-id", tenant);
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
        return res;
    }
}
