namespace Bagile.EtlService.Models
{
    public class CanonicalWooOrderDto
    {
        public long RawOrderId { get; init; }

        public long OrderId { get; set; }           // set by WooOrderService after upsert
        public string ExternalId { get; init; } = "";

        public string BillingEmail { get; init; } = "";
        public string BillingName { get; init; } = "";
        public string BillingCompany { get; init; } = "";

        public int TotalQuantity { get; init; }
        public decimal SubTotal { get; init; }
        public decimal TotalTax { get; init; }
        public decimal Total { get; init; }         // Woo "total"
        public decimal PaymentTotal { get; init; }  // normally same as Total
        public decimal RefundTotal { get; init; }   // sum of refunds
        public string Currency { get; init; } = "GBP";

        public string Status { get; init; } = "pending";

        public bool HasFooEventsMetadata { get; init; }

        public DateTime? DateCreated { get; init; }

        public string RawPayload { get; init; } = "";
        public List<CanonicalTicketDto> Tickets { get; set; } = new();
    }
}