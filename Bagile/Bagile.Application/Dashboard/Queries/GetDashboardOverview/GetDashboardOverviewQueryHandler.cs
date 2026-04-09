using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Analytics.Queries.GetRevenueSummary;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Dashboard.DTOs;
using MediatR;

namespace Bagile.Application.Dashboard.Queries.GetDashboardOverview;

public class GetDashboardOverviewQueryHandler
    : IRequestHandler<GetDashboardOverviewQuery, DashboardOverviewDto>
{
    private readonly IMediator _mediator;
    private readonly ICourseScheduleQueries _courseQueries;
    private readonly ITransferQueries _transferQueries;

    public GetDashboardOverviewQueryHandler(
        IMediator mediator,
        ICourseScheduleQueries courseQueries,
        ITransferQueries transferQueries)
    {
        _mediator = mediator;
        _courseQueries = courseQueries;
        _transferQueries = transferQueries;
    }

    public async Task<DashboardOverviewDto> Handle(
        GetDashboardOverviewQuery request,
        CancellationToken ct)
    {
        // IncludeBreakdowns=false skips the expensive correlated subqueries
        var revenueTask = _mediator.Send(new GetRevenueSummaryQuery(IncludeBreakdowns: false), ct);
        var monitoringTask = _courseQueries.GetCourseMonitoringDataAsync(60, ct);
        var transfersTask = _transferQueries.GetPendingTransfersAsync(ct);

        await Task.WhenAll(revenueTask, monitoringTask, transfersTask);

        return new DashboardOverviewDto
        {
            Revenue = await revenueTask,
            Monitoring = await monitoringTask,
            PendingTransferCount = (await transfersTask).Count(),
        };
    }
}
