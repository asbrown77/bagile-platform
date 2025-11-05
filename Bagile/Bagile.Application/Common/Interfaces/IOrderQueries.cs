using Bagile.Application.Orders.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface IOrderQueries
{
    Task<IEnumerable<OrderDto>> GetOrdersAsync(
        string? status,
        DateTime? from,
        DateTime? to,
        string? email,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task<int> CountOrdersAsync(
        string? status,
        DateTime? from,
        DateTime? to,
        string? email,
        CancellationToken ct = default);

    Task<OrderDetailDto?> GetOrderByIdAsync(long orderId, CancellationToken ct = default);
}