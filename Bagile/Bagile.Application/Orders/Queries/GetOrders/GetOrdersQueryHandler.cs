using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Common.Models;
using Bagile.Application.Orders.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Orders.Queries.GetOrders;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, PagedResult<OrderDto>>
{
    private readonly IOrderQueries _queries;
    private readonly ILogger<GetOrdersQueryHandler> _logger;

    public GetOrdersQueryHandler(IOrderQueries queries, ILogger<GetOrdersQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<PagedResult<OrderDto>> Handle(GetOrdersQuery request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching orders: Status={Status}, From={From}, To={To}, Email={Email}, Page={Page}",
            request.Status, request.From, request.To, request.Email, request.Page);

        var orders = await _queries.GetOrdersAsync(
            request.Status,
            request.From,
            request.To,
            request.Email,
            request.Page,
            request.PageSize,
            ct);

        var totalCount = await _queries.CountOrdersAsync(
            request.Status,
            request.From,
            request.To,
            request.Email,
            ct);

        return new PagedResult<OrderDto>
        {
            Items = orders,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}