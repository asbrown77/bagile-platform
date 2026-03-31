using System.Globalization;
using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Common.Interfaces;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetRevenueMonthDrilldown;

public class GetRevenueMonthDrilldownQueryHandler
    : IRequestHandler<GetRevenueMonthDrilldownQuery, MonthDrilldownDto>
{
    private readonly IRevenueQueries _queries;

    public GetRevenueMonthDrilldownQueryHandler(IRevenueQueries queries)
    {
        _queries = queries;
    }

    public async Task<MonthDrilldownDto> Handle(
        GetRevenueMonthDrilldownQuery request,
        CancellationToken ct)
    {
        var orders = (await _queries.GetMonthDrilldownAsync(
            request.Year, request.Month, ct)).ToList();

        var monthName = new DateTime(request.Year, request.Month, 1)
            .ToString("MMMM", CultureInfo.InvariantCulture);

        return new MonthDrilldownDto
        {
            Year = request.Year,
            Month = request.Month,
            MonthName = monthName,
            TotalRevenue = orders.Sum(o => o.NetRevenue),
            TotalOrders = orders.Select(o => o.OrderId).Distinct().Count(),
            Orders = orders
        };
    }
}
