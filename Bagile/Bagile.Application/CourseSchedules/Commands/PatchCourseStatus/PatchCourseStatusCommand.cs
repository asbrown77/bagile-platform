using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.PatchCourseStatus;

public record PatchCourseStatusCommand(long Id, string Status) : IRequest<bool>;
