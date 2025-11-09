namespace Bagile.Application.CourseSchedules.DTOs;

/// <summary>
/// Course schedule list item with computed fields
/// </summary>
public record CourseScheduleDto
{
    public long Id { get; init; }
    public string CourseCode { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string? Location { get; init; }
    public string? Type { get; init; }              // public/private
    public string? Status { get; init; }            // published/draft/cancelled/completed
    public int CurrentEnrolmentCount { get; init; }
    public bool GuaranteedToRun { get; init; }      // e.g., enrolments >= 3
    public bool NeedsAttention { get; init; }       // e.g., starts in <= 7 days AND enrolments < 3
}