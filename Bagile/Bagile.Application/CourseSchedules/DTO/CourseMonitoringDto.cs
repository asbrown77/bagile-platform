namespace Bagile.Application.CourseSchedules.DTOs;

public record CourseMonitoringDto
{
    public long Id { get; init; }
    public string CourseCode { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? TrainerName { get; init; }
    public string? Location { get; init; }
    public int CurrentEnrolmentCount { get; init; }
    public int MinimumRequired { get; init; }
    public double FillPercentage { get; init; }
    public string MonitoringStatus { get; init; } = "";  // healthy, at_risk, critical, cancelled
    public DateTime? DecisionDeadline { get; init; }
    public int DaysUntilStart { get; init; }
    public int DaysUntilDecision { get; init; }
    public string RecommendedAction { get; init; } = "";
}
