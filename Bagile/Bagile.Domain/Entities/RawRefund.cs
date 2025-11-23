namespace Bagile.Domain.Entities
{
    public class RawRefund
    {
        public long Id { get; set; }
        public long WooOrderId { get; set; }
        public long RefundId { get; set; }
        public decimal RefundTotal { get; set; }
        public string? RefundReason { get; set; }
        public string LineItemsJson { get; set; } = "[]";
        public string RawJson { get; set; } = "{}";
        public DateTime CreatedAt { get; set; }
    }
}