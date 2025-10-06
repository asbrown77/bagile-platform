using System;

namespace Bagile.Infrastructure.Models
{
    public class RawOrder
    {
        public int Id { get; set; }
        public string Source { get; set; } = string.Empty;
        public string ExternalId { get; set; } = string.Empty;
        public string Payload { get; set; } = string.Empty;

        public DateTime ReceivedAt { get; set; }   
        public string? EventType { get; set; }     
    }
}