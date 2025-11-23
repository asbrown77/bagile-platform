public class CanonicalXeroInvoiceDto
{
    public long RawOrderId { get; set; }
    public string InvoiceId { get; set; } = "";

    public string ExternalId { get; set; } = "";

    public string Status { get; set; } = "";

    public string BillingEmail { get; set; } = "";
    public string BillingName { get; set; } = "";
    public string BillingCompany { get; set; } = "";
    public string Reference { get; set; } = "";

    public decimal SubTotal { get; set; }
    public decimal TotalTax { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal AmountDue { get; set; }
    
    public string Currency { get; set; } = "GBP";

    public DateTime? InvoiceDate { get; set; }

    public string RawPayload { get; set; } = "";

}