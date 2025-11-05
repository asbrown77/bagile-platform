using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Orders.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDetailDto?>
{
    private readonly IOrderQueries _queries;
    private readonly ILogger<GetOrderByIdQueryHandler> _logger;

    public GetOrderByIdQueryHandler(IOrderQueries queries, ILogger<GetOrderByIdQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<OrderDetailDto?> Handle(GetOrderByIdQuery request, CancellationToken ct)
    {
        _logger.LogInformation("Fetching order {OrderId}", request.OrderId);
        return await _queries.GetOrderByIdAsync(request.OrderId, ct);
    }
}