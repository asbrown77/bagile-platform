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
        int year         = request.Year ?? DateTime.UtcNow.Year;
        int previousYear = year - 1;
        int currentMonth = DateTime.UtcNow.Month;

        // Always fetch monthly totals (fast — simple GROUP BY on orders)
        var currentYearMonthlyTask  = _queries.GetMonthlyRevenueAsync(year, ct);
        var previousYearMonthlyTask = _queries.GetMonthlyRevenueAsync(previousYear, ct);

        // Breakdown queries (byCourseType, bySource, byCountry) are expensive —
        // correlated subqueries over enrolments. Only run for the Revenue page,
        // not the dashboard overview.
        Task<IEnumerable<CourseTypeRevenueDto>>? byCourseTypeTask = null;
        Task<IEnumerable<SourceRevenueDto>>?     bySourceTask     = null;
        Task<IEnumerable<CountryRevenueDto>>?    byCountryTask    = null;

        if (request.IncludeBreakdowns)
        {
            byCourseTypeTask = _queries.GetRevenueByCourseTypeAsync(year, ct);
            bySourceTask     = _queries.GetRevenueBySourceAsync(year, ct);
            byCountryTask    = _queries.GetRevenueByCountryAsync(year, ct);
        }

        var tasksToAwait = request.IncludeBreakdowns
            ? new Task[] { currentYearMonthlyTask, previousYearMonthlyTask, byCourseTypeTask!, bySourceTask!, byCountryTask! }
            : new Task[] { currentYearMonthlyTask, previousYearMonthlyTask };

        await Task.WhenAll(tasksToAwait);

        var currentYearMonthly  = (await currentYearMonthlyTask).ToList();
        var previousYearMonthly = (await previousYearMonthlyTask).ToList();

        var currentMonthData = currentYearMonthly.FirstOrDefault(m => m.Month == currentMonth);

        var maxMonth = currentYearMonthly.Any()
            ? currentYearMonthly.Max(m => m.Month)
            : currentMonth;
        var previousYearYtd = previousYearMonthly
            .Where(m => m.Month <= maxMonth)
            .Sum(m => m.Revenue);

        return new RevenueSummaryDto
        {
            CurrentMonthRevenue    = currentMonthData?.Revenue ?? 0,
            CurrentMonthOrders     = currentMonthData?.OrderCount ?? 0,
            CurrentYearRevenue     = currentYearMonthly.Sum(m => m.Revenue),
            CurrentYearOrders      = currentYearMonthly.Sum(m => m.OrderCount),
            PreviousYearRevenue    = previousYearMonthly.Sum(m => m.Revenue),
            PreviousYearYtdRevenue = previousYearYtd,
            MonthlyBreakdown       = currentYearMonthly,
            PreviousYearMonthly    = previousYearMonthly,
            ByCourseType           = byCourseTypeTask  != null ? await byCourseTypeTask  : [],
            BySource               = bySourceTask      != null ? await bySourceTask       : [],
            ByCountry              = byCountryTask     != null ? await byCountryTask      : [],
        };
    }
}
