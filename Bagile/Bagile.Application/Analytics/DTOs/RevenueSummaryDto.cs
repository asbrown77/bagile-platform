namespace Bagile.Application.Analytics.DTOs;

public record RevenueSummaryDto
{
    public decimal CurrentMonthRevenue { get; init; }
    public decimal CurrentYearRevenue { get; init; }
    public decimal PreviousYearRevenue { get; init; }
    public int CurrentMonthOrders { get; init; }
    public int CurrentYearOrders { get; init; }
    public IEnumerable<MonthlyRevenueDto> MonthlyBreakdown { get; init; } = [];
    public IEnumerable<CourseTypeRevenueDto> ByCourseType { get; init; } = [];
    public IEnumerable<MonthlyRevenueDto> PreviousYearMonthly { get; init; } = [];
    public IEnumerable<SourceRevenueDto> BySource { get; init; } = [];
}

public record MonthlyRevenueDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = "";
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
    public int AttendeeCount { get; init; }
}

public record SourceRevenueDto
{
    public string Source { get; init; } = "";
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
    public int AttendeeCount { get; init; }
}

public record CourseTypeRevenueDto
{
    public string CourseType { get; init; } = "";
    public decimal Revenue { get; init; }
    public int OrderCount { get; init; }
    public int AttendeeCount { get; init; }
}
