using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Bagile.Infrastructure.Models;

namespace Bagile.Infrastructure.Clients
{
    public interface IWooApiClient
    {
        Task<IReadOnlyList<string>> FetchOrdersAsync(DateTime? modifiedSince, CancellationToken ct);
        Task<IReadOnlyList<string>> FetchOrdersAsync(int page, int perPage, DateTime? modifiedSince, CancellationToken ct);
        Task<IReadOnlyList<WooProductDto>> FetchProductsAsync(DateTime? modifiedSince = null, CancellationToken ct = default);

        /// <summary>
        /// Search products by keyword (name/SKU). Returns up to perPage results.
        /// </summary>
        Task<IReadOnlyList<WooProductDto>> SearchProductsAsync(string keyword, int perPage = 10, string status = "publish", CancellationToken ct = default);

        /// <summary>
        /// Get full product JSON (all fields including meta_data) for a single product.
        /// </summary>
        Task<JsonDocument?> GetProductFullAsync(long productId, CancellationToken ct = default);

        /// <summary>
        /// Create a new WooCommerce product. Returns the created product as raw JSON.
        /// </summary>
        Task<JsonDocument?> CreateProductAsync(JsonElement productPayload, CancellationToken ct = default);

        /// <summary>
        /// Update an existing WooCommerce product. Returns the updated product as raw JSON.
        /// </summary>
        Task<JsonDocument?> UpdateProductAsync(long productId, JsonElement updatePayload, CancellationToken ct = default);

        /// <summary>
        /// Get all product tags. Returns tag ID, name, slug.
        /// </summary>
        Task<IReadOnlyList<WooTagDto>> GetAllTagsAsync(CancellationToken ct = default);

        /// <summary>
        /// Returns true if the product exists in WooCommerce (status publish/draft/etc.),
        /// false if the product returns 404 or has been trashed.
        /// Throws <see cref="HttpRequestException"/> on transient / server errors so the
        /// caller can distinguish a missing product from a network failure.
        /// </summary>
        Task<bool> ProductExistsAsync(long productId, CancellationToken ct = default);
    }

    public class WooTagDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public long Id { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [System.Text.Json.Serialization.JsonPropertyName("slug")]
        public string Slug { get; set; } = "";
    }
}
