namespace Bagile.Application.CourseSchedules.DTOs;

/// <summary>
/// Raw data from SQL for the monitoring query — business logic applied in the handler
/// </summary>
public record CourseMonitoringRawDto
{
    public long Id { get; init; }
    public string CourseCode { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? TrainerName { get; init; }
    public string? Location { get; init; }
    public string? Status { get; init; }
    public int CurrentEnrolmentCount { get; init; }
}
