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
            query.Append($"&modified_after={since.Value:yyyy-MM-ddTHH:mm:ss}Z");

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
    // Search Products
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<WooProductDto>> SearchProductsAsync(
        string keyword,
        int perPage = 10,
        string status = "publish",
        CancellationToken ct = default)
    {
        var url = $"/wp-json/wc/v3/products?search={Uri.EscapeDataString(keyword)}&per_page={perPage}&status={status}&orderby=date&order=desc";
        _logger.LogInformation("Searching Woo products: {Url}", url);

        var items = await SafeGet<List<WooProductDto>>(url, ct);
        return items?.AsReadOnly() ?? (IReadOnlyList<WooProductDto>)Array.Empty<WooProductDto>();
    }

    // ------------------------------------------------------------
    // Get Full Product (raw JSON with all meta)
    // ------------------------------------------------------------
    public async Task<JsonDocument?> GetProductFullAsync(long productId, CancellationToken ct = default)
    {
        var url = $"/wp-json/wc/v3/products/{productId}";
        _logger.LogInformation("Fetching full Woo product {ProductId}", productId);
        return await SafeGet<JsonDocument>(url, ct);
    }

    // ------------------------------------------------------------
    // Create Product
    // ------------------------------------------------------------
    public async Task<JsonDocument?> CreateProductAsync(JsonElement productPayload, CancellationToken ct = default)
    {
        const string url = "/wp-json/wc/v3/products";
        _logger.LogInformation("Creating Woo product");

        try
        {
            var content = new StringContent(
                productPayload.GetRawText(),
                Encoding.UTF8,
                "application/json");

            var response = await _http.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("WooCommerce create product failed: {Status} {Body}",
                    response.StatusCode, body);
                return null;
            }

            return JsonDocument.Parse(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Woo product");
            return null;
        }
    }

    // ------------------------------------------------------------
    // Update Product
    // ------------------------------------------------------------
    public async Task<JsonDocument?> UpdateProductAsync(long productId, JsonElement updatePayload, CancellationToken ct = default)
    {
        var url = $"/wp-json/wc/v3/products/{productId}";
        _logger.LogInformation("Updating Woo product {ProductId}", productId);

        try
        {
            var content = new StringContent(
                updatePayload.GetRawText(),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
            var response = await _http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("WooCommerce update product failed: {Status} {Body}",
                    response.StatusCode, body);
                return null;
            }

            return JsonDocument.Parse(body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Woo product {ProductId}", productId);
            return null;
        }
    }

    // ------------------------------------------------------------
    // Get All Tags
    // ------------------------------------------------------------
    public async Task<IReadOnlyList<WooTagDto>> GetAllTagsAsync(CancellationToken ct = default)
    {
        var url = "/wp-json/wc/v3/products/tags?per_page=100";
        _logger.LogInformation("Fetching Woo product tags");

        var tags = await SafeGet<List<WooTagDto>>(url, ct);
        return tags?.AsReadOnly() ?? (IReadOnlyList<WooTagDto>)Array.Empty<WooTagDto>();
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
