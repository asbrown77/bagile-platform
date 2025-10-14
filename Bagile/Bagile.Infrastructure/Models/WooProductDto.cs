using System.Text.Json.Serialization;

namespace Bagile.Infrastructure.Models;

public class WooProductDto
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("sku")] public string? Sku { get; set; }

    [JsonPropertyName("price")]
    [JsonConverter(typeof(NullableDecimalConverter))]
    public decimal? Price { get; set; }

    [JsonPropertyName("stock_quantity")] public int? StockQuantity { get; set; }

    [JsonPropertyName("meta_data")] public List<WooMetaData>? MetaData { get; set; }
}

public class WooMetaData
{
    [JsonPropertyName("key")] public string? Key { get; set; }
    [JsonPropertyName("value")] public object? Value { get; set; }
}