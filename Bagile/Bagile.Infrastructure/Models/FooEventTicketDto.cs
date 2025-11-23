using System.Text.Json.Serialization;

namespace Bagile.Infrastructure.Models;

public class FooEventTicketDto
{
    [JsonPropertyName("ticket_id")]
    public long? TicketId { get; set; }

    [JsonPropertyName("ticket_number")]
    public string? TicketNumber { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("event_id")]
    public long EventId { get; set; }

    [JsonPropertyName("event_name")]
    public string? EventName { get; set; }

    [JsonPropertyName("event_start")]
    public DateTimeOffset? EventStart { get; set; }

    [JsonPropertyName("attendee_name")]
    public string? AttendeeName { get; set; }

    [JsonPropertyName("attendee_email")]
    public string AttendeeEmail { get; set; } = string.Empty;

    [JsonPropertyName("designation")]
    public string? Designation { get; set; }

    [JsonPropertyName("meta")]
    public object? Meta { get; set; }

    [JsonPropertyName("product_id")]
    public int ProductId { get; set; }
}