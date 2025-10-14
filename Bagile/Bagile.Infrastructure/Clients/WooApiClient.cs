using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bagile.Infrastructure.Models;
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


    // ✅ existing overload kept for compatibility
    public Task<IReadOnlyList<string>> FetchOrdersAsync(DateTime? since = null, CancellationToken ct = default)
        => FetchOrdersAsync(page: 1, perPage: 100, since, ct);

    // ✅ new paged version
    public async Task<IReadOnlyList<string>> FetchOrdersAsync(
        int page,
        int perPage,
        DateTime? since = null,
        CancellationToken ct = default)
    {
        // Build base query
        var query = new StringBuilder($"/wp-json/wc/v3/orders?page={page}&per_page={perPage}");

        if (since != null)
            query.Append($"&after={since.Value:yyyy-MM-ddTHH:mm:ss}Z");

        var url = query.ToString();
        _logger.LogInformation("Fetching Woo orders: {Url}", url);

        var response = await _http.GetFromJsonAsync<JsonDocument>(url, ct);
        var results = new List<string>();

        if (response?.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in response.RootElement.EnumerateArray())
                results.Add(item.GetRawText());
        }
        else
        {
            _logger.LogWarning("Unexpected response from WooCommerce: {Json}", response);
        }

        _logger.LogInformation("Fetched {Count} Woo orders (page {Page})", results.Count, page);
        return results;
    }

    public async Task<IReadOnlyList<WooProduct>> FetchProductsAsync(CancellationToken ct = default)
    {
        const string url = "/wp-json/wc/v3/products?per_page=100";
        _logger.LogInformation("Fetching Woo products: {Url}", url);

        var response = await _http.GetFromJsonAsync<List<WooProduct>>(url, ct);

        if (response == null || response.Count == 0)
        {
            _logger.LogWarning("No Woo products returned");
            return Array.Empty<WooProduct>();
        }

        _logger.LogInformation("Fetched {Count} Woo products", response.Count);
        return response;
    }

}
