using Bagile.Infrastructure.Models;

namespace Bagile.Infrastructure.Clients;

public interface IFooEventsTicketsClient
{
    Task<IReadOnlyList<FooEventTicketDto>> FetchTicketsForOrderAsync(
        int orderId,
        CancellationToken ct = default);
}