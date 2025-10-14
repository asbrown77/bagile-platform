namespace Bagile.Infrastructure.Models;

public class WooProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // e.g. "publish"
    public string? Sku { get; set; }
    public decimal? Price { get; set; }
    public int? StockQuantity { get; set; }

    // Meta-data section (custom fields)
    public WooProductMeta? Meta { get; set; }
}

public class WooProductMeta
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? TrainerName { get; set; }
    public string? FormatType { get; set; } // "virtual" / "in_person"
}