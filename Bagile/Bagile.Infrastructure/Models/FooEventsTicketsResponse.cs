namespace Bagile.Infrastructure.Models;

public class FooEventsTicketsResponse
{
    public string? Currency { get; set; }

    public List<FooEventTicketDto> Tickets { get; set; } = new();
}