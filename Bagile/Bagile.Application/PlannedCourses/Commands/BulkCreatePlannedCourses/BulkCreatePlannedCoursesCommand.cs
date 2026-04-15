using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.BulkCreatePlannedCourses;

public record BulkCreatePlannedCoursesCommand : IRequest<BulkCreatePlannedCoursesResult>
{
    public IReadOnlyList<BulkCourseRow> Courses { get; init; } = [];
}

public record BulkCourseRow
{
    public string CourseType { get; init; } = "";
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TrainerId { get; init; }
    public bool IsVirtual { get; init; } = true;
    public string? Venue { get; init; }
    public string? Notes { get; init; }
    public DateTime? DecisionDeadline { get; init; }
    public bool IsPrivate { get; init; }
}

public record BulkCreatePlannedCoursesResult
{
    public IReadOnlyList<BulkRowResult> Results { get; init; } = [];
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailureCount => Results.Count(r => !r.Success);
}

public record BulkRowResult
{
    public int Index { get; init; }
    public bool Success { get; init; }
    public int? Id { get; init; }
    public string? Error { get; init; }
}
