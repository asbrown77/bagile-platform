using Bagile.Application.PlannedCourses.DTOs;
using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.CreatePlannedCourse;

public record CreatePlannedCourseCommand : IRequest<PlannedCourseDto>
{
    public string CourseType { get; init; } = "";
    public int TrainerId { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsVirtual { get; init; } = true;
    public string? Venue { get; init; }
    public string? Notes { get; init; }
    public DateTime? DecisionDeadline { get; init; }
    public bool IsPrivate { get; init; }
}
