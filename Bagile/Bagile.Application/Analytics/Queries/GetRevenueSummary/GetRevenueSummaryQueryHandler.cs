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

        var currentYearMonthlyTask  = _queries.GetMonthlyRevenueAsync(year, ct);
        var previousYearMonthlyTask = _queries.GetMonthlyRevenueAsync(previousYear, ct);
        var byCourseTypeTask        = _queries.GetRevenueByCourseTypeAsync(year, ct);
        var bySourceTask            = _queries.GetRevenueBySourceAsync(year, ct);
        var byCountryTask           = _queries.GetRevenueByCountryAsync(year, ct);

        await Task.WhenAll(currentYearMonthlyTask, previousYearMonthlyTask,
                           byCourseTypeTask, bySourceTask, byCountryTask);

        var currentYearMonthly  = (await currentYearMonthlyTask).ToList();
        var previousYearMonthly = (await previousYearMonthlyTask).ToList();
        var byCourseType        = await byCourseTypeTask;
        var bySource            = await bySourceTask;
        var byCountry           = await byCountryTask;

        int currentMonth = DateTime.UtcNow.Month;

        var currentMonthData = currentYearMonthly
            .FirstOrDefault(m => m.Month == currentMonth);

        // Fair YTD comparison: only compare months that have elapsed
        var maxMonth = currentYearMonthly.Any()
            ? currentYearMonthly.Max(m => m.Month)
            : currentMonth;
        var previousYearYtd = previousYearMonthly
            .Where(m => m.Month <= maxMonth)
            .Sum(m => m.Revenue);

        return new RevenueSummaryDto
        {
            CurrentMonthRevenue = currentMonthData?.Revenue ?? 0,
            CurrentMonthOrders = currentMonthData?.OrderCount ?? 0,
            CurrentYearRevenue = currentYearMonthly.Sum(m => m.Revenue),
            CurrentYearOrders = currentYearMonthly.Sum(m => m.OrderCount),
            PreviousYearRevenue = previousYearMonthly.Sum(m => m.Revenue),
            PreviousYearYtdRevenue = previousYearYtd,
            MonthlyBreakdown = currentYearMonthly,
            PreviousYearMonthly = previousYearMonthly,
            ByCourseType = byCourseType,
            BySource = bySource,
            ByCountry = byCountry
        };
    }
}
