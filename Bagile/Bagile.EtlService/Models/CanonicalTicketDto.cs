using System.Text.Json;

namespace Bagile.EtlService.Models;

public class CanonicalTicketDto
{
    public string TicketId { get; init; } = string.Empty;
    public string EventId { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;

    public long? ProductId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Designation { get; init; } = string.Empty;
    public bool IsCancelled { get; init; }

    public JsonElement RawLineItem { get; init; }
    public JsonElement RawOrderPayload { get; init; }
}