using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Common.Interfaces;
using MediatR;

namespace Bagile.Application.Analytics.Queries.GetRevenueSummary;

public class GetRevenueSummaryQueryHandler
    : IRequestHandler<GetRevenueSummaryQuery, RevenueSummaryDto>
{
    private readonly IRevenueQueries _queries;

    public GetRevenueSummaryQueryHandler(IRevenueQueries queries)
    {
        _queries = queries;
    }

    public async Task<RevenueSummaryDto> Handle(
        GetRevenueSummaryQuery request,
        CancellationToken ct)
    {
        int year = request.Year ?? DateTime.UtcNow.Year;
        int previousYear = year - 1;

        var currentYearMonthly = (await _queries.GetMonthlyRevenueAsync(year, ct)).ToList();
        var previousYearMonthly = (await _queries.GetMonthlyRevenueAsync(previousYear, ct)).ToList();
        var byCourseType = await _queries.GetRevenueByCourseTypeAsync(year, ct);
        var bySource = await _queries.GetRevenueBySourceAsync(year, ct);

        int currentMonth = DateTime.UtcNow.Month;

        var currentMonthData = currentYearMonthly
            .FirstOrDefault(m => m.Month == currentMonth);

        return new RevenueSummaryDto
        {
            CurrentMonthRevenue = currentMonthData?.Revenue ?? 0,
            CurrentMonthOrders = currentMonthData?.OrderCount ?? 0,
            CurrentYearRevenue = currentYearMonthly.Sum(m => m.Revenue),
            CurrentYearOrders = currentYearMonthly.Sum(m => m.OrderCount),
            PreviousYearRevenue = previousYearMonthly.Sum(m => m.Revenue),
            MonthlyBreakdown = currentYearMonthly,
            PreviousYearMonthly = previousYearMonthly,
            ByCourseType = byCourseType,
            BySource = bySource
        };
    }
}
