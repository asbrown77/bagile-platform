using MediatR;
using Bagile.Application.Orders.DTOs;

namespace Bagile.Application.Orders.Queries.GetOrderById;

public record GetOrderByIdQuery(long OrderId) : IRequest<OrderDetailDto?>;