namespace Bagile.Domain.Entities;

public class RawPayment
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public string Source { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GBP";
    public string RawJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
}