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

    // ------------------------------------------------------------
    // Fetch Orders
    // ------------------------------------------------------------
    public Task<IReadOnlyList<string>> FetchOrdersAsync(DateTime? since = null, CancellationToken ct = default)
        => FetchOrdersAsync(page: 1, perPage: 100, since, ct);

    public async Task<IReadOnlyList<string>> FetchOrdersAsync(
        int page,
        int perPage,
        DateTime? since = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"/wp-json/wc/v3/orders?page={page}&per_page={perPage}");

        if (since != null)
            query.Append($"&after={since.Value:yyyy-MM-ddTHH:mm:ss}Z");

        var url = query.ToString();
        _logger.LogInformation("Fetching Woo orders: {Url}", url);

        var json = await SafeGet<JsonDocument>(url, ct);
        var results = new List<string>();

        if (json != null && json.RootElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in json.RootElement.EnumerateArray())
                results.Add(item.GetRawText());
        }
        else
        {
            _logger.LogWarning("Unexpected Woo order response: {Json}", json);
        }

        _logger.LogInformation("Fetched {Count} Woo orders (page {Page})", results.Count, page);
        return results;
    }

    // ------------------------------------------------------------
    // Fetch Products (pagination + draft + publish)
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<WooProductDto>> FetchProductsAsync(
        DateTime? modifiedSince = null,
        CancellationToken ct = default)
    {
        var allProducts = new List<WooProductDto>();
        var statuses = new[] { "publish", "draft" };
        const int pageSize = 100;

        foreach (var status in statuses)
        {
            var page = 1;

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                var url = BuildProductUrl(page, pageSize, status, modifiedSince);

                _logger.LogInformation(
                    "Fetching Woo products page {Page} status {Status}: {Url}",
                    page, status, url);

                var items = await SafeGet<List<WooProductDto>>(url, ct);

                if (items == null || items.Count == 0)
                {
                    _logger.LogInformation(
                        "No more products for status {Status} after page {Page}",
                        status, page);
                    break;
                }

                allProducts.AddRange(items);

                if (items.Count < pageSize)
                    break;

                page++;
            }
        }

        var distinct = allProducts
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToList();

        _logger.LogInformation("Fetched total {Count} distinct Woo products.", distinct.Count);

        return distinct;
    }

    // ------------------------------------------------------------
    // SAFE GETTER (fixes your ObjectDisposedException)
    // ------------------------------------------------------------
    private async Task<T?> SafeGet<T>(string url, CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync(ct);

            return await JsonSerializer.DeserializeAsync<T>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Woo API url={Url}", url);
            return default;
        }
    }

    // ------------------------------------------------------------
    // URL builder
    // ------------------------------------------------------------
    private string BuildProductUrl(
        int page,
        int pageSize,
        string status,
        DateTime? modifiedSince)
    {
        var sb = new StringBuilder("/wp-json/wc/v3/products");
        sb.Append($"?page={page}");
        sb.Append($"&per_page={pageSize}");
        sb.Append($"&status={status}");

        if (modifiedSince != null)
            sb.Append($"&modified_after={modifiedSince.Value:yyyy-MM-ddTHH:mm:ss}Z");

        return sb.ToString();
    }
}
