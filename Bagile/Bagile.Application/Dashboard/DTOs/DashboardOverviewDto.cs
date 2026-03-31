using Bagile.Application.Analytics.DTOs;
using Bagile.Application.CourseSchedules.DTOs;

namespace Bagile.Application.Dashboard.DTOs;

public record DashboardOverviewDto
{
    public RevenueSummaryDto Revenue { get; init; } = null!;
    public IEnumerable<CourseMonitoringRawDto> Monitoring { get; init; } = [];
    public int PendingTransferCount { get; init; }
}
