namespace Bagile.Application.Analytics.DTOs;

public record CourseDemandDto
{
    public string CourseType { get; init; } = "";
    public int CoursesRun { get; init; }
    public int TotalEnrolments { get; init; }
    public decimal AvgAttendees { get; init; }
    public decimal AvgFillPct { get; init; }
}

public record CourseDemandMonthlyDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string CourseType { get; init; } = "";
    public int Enrolments { get; init; }
}

public record CourseDemandResultDto
{
    public int LookbackMonths { get; init; }
    public IEnumerable<CourseDemandDto> CourseTypes { get; init; } = [];
    public IEnumerable<CourseDemandMonthlyDto> MonthlyTrend { get; init; } = [];
}
