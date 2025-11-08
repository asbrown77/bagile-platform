using Bagile.Infrastructure.Models;

namespace Bagile.Infrastructure.Clients;

public interface IFooEventsTicketsClient
{
    Task<IReadOnlyList<FooEventTicketDto>> FetchTicketsForOrderAsync(
        string orderId,
        CancellationToken ct = default);
}