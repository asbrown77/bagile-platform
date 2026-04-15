using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.DeleteCourseSchedule;

/// <summary>
/// Hard-deletes a course schedule with no enrolments.
/// Returns <see cref="DeleteCourseScheduleResult"/> describing the outcome.
/// </summary>
public record DeleteCourseScheduleCommand(long Id) : IRequest<DeleteCourseScheduleResult>;

public record DeleteCourseScheduleResult(bool Deleted, string? Error);
