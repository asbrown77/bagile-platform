using MediatR;
using Bagile.Application.Common.Models;
using Bagile.Application.Orders.DTOs;

namespace Bagile.Application.Orders.Queries.GetOrders;

public record GetOrdersQuery : IRequest<PagedResult<OrderDto>>
{
    public string? Status { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Email { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}