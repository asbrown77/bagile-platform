namespace Bagile.Domain.Entities;

public class Order
{
    public long RawOrderId { get; set; }
    public string ExternalId { get; set; } = "";
    public string Source { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Reference { get; set; } = "";
    public string? BillingCompany { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? OrderDate { get; set; }
}