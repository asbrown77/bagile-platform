using MediatR;

namespace Bagile.Application.PlannedCourses.Commands.DeletePlannedCourse;

/// <summary>
/// Delete a planned course. Returns true if deleted, false if not found.
/// Throws InvalidOperationException if the course has publications (409 scenario).
/// </summary>
public record DeletePlannedCourseCommand(int Id) : IRequest<DeletePlannedCourseResult>;

public record DeletePlannedCourseResult(bool Found, bool HasPublications);
