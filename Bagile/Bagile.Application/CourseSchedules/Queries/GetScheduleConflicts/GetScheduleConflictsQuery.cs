using MediatR;

namespace Bagile.Application.CourseSchedules.Queries.GetScheduleConflicts;

public record GetScheduleConflictsQuery(
    DateTime StartDate,
    DateTime EndDate,
    string? TrainerName = null
) : IRequest<IEnumerable<ScheduleConflictDto>>;

public record ScheduleConflictDto
{
    public long ConflictingCourseId { get; init; }
    public string CourseName { get; init; } = "";
    public string CourseCode { get; init; } = "";
    public DateTime? StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string Type { get; init; } = "";
    public string? TrainerName { get; init; }
    public int EnrolmentCount { get; init; }
    public bool IsGuaranteedToRun { get; init; }
    public string ConflictType { get; init; } = "";
}
