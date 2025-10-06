using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bagile.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bagile.Infrastructure.Clients;

public class WooApiClient : IWooApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<WooApiClient> _logger;

    public WooApiClient(HttpClient http, IConfiguration cfg, ILogger<WooApiClient> logger)
    {
        _http = http;
        _logger = logger;

        var baseUrl = cfg["WooCommerce:BaseUrl"]!;
        var key = cfg["WooCommerce:ConsumerKey"]!;
        var secret = cfg["WooCommerce:ConsumerSecret"]!;
        var creds = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{key}:{secret}"));

        _http.BaseAddress = new Uri(baseUrl);
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", creds);
    }

    public async Task<IReadOnlyList<string>> FetchOrdersAsync(DateTime? since = null, CancellationToken ct = default)
    {
        var url = "/wp-json/wc/v3/orders";
        if (since != null)
            url += $"?after={since.Value:yyyy-MM-ddTHH:mm:ss}Z";

        var response = await _http.GetFromJsonAsync<JsonDocument>(url, ct);
        var results = new List<string>();

        if (response?.RootElement.ValueKind == JsonValueKind.Array)
            foreach (var item in response.RootElement.EnumerateArray())
                results.Add(item.GetRawText());

        _logger.LogInformation("Fetched {Count} Woo orders", results.Count);
        return results;
    }
}