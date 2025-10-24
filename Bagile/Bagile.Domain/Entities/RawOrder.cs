namespace Bagile.Domain.Entities
{
    public class RawOrder
    {
        public int Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;
        public string PayloadHash { get; set; } = string.Empty;
        public string? EventType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}