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
    public int? TotalQuantity { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TotalTax { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Status { get; set; }
    public DateTime? OrderDate { get; set; }
    public decimal? PaymentTotal { get; set; }      
    public decimal RefundTotal { get; set; }        
    public decimal NetTotal { get; set; }           // PaymentTotal - RefundTotal
    public string LifecycleStatus { get; set; } = "pending"; // pending, completed, partially_refunded, fully_refunded, cancelled
    public string Currency { get; set; } = "GBP";
}