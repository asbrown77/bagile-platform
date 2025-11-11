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

    public Task<IReadOnlyList<string>> FetchOrdersAsync(DateTime? since = null, CancellationToken ct = default)
        => FetchOrdersAsync(page: 1, perPage: 100, since, ct);

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


    public async Task<IReadOnlyList<WooProductDto>> FetchProductsAsync(
        DateTime? modifiedSince = null,
        CancellationToken ct = default)
    {
        var allProducts = new List<WooProductDto>();
        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            ct.ThrowIfCancellationRequested();

            // Build URL with pagination
            var queryBuilder = new StringBuilder($"/wp-json/wc/v3/products?page={page}&per_page={pageSize}");

            // Optional: only fetch published products (exclude drafts, private, etc.)
            queryBuilder.Append("&status=publish");

            // Optional: incremental sync - only fetch modified products
            if (modifiedSince != null)
            {
                queryBuilder.Append($"&modified_after={modifiedSince.Value:yyyy-MM-ddTHH:mm:ss}Z");
            }

            var url = queryBuilder.ToString();
            _logger.LogInformation("Fetching Woo products page {Page}: {Url}", page, url);

            var response = await _http.GetFromJsonAsync<List<WooProductDto>>(url, ct);

            if (response == null || response.Count == 0)
            {
                _logger.LogInformation("No more products after page {Page}", page);
                break;
            }

            allProducts.AddRange(response);
            _logger.LogInformation("Fetched {Count} products from page {Page}", response.Count, page);

            // If we got fewer than pageSize, we've reached the last page
            if (response.Count < pageSize)
            {
                _logger.LogInformation("Reached last page ({Page}), stopping pagination", page);
                break;
            }

            page++;
        }

        _logger.LogInformation("✅ Fetched total of {Count} WooCommerce products across {Pages} pages",
            allProducts.Count, page);

        return allProducts;
    }
}