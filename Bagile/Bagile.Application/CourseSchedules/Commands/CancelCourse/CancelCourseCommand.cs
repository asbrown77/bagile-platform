using Bagile.Application.CourseSchedules.DTOs;
using MediatR;

namespace Bagile.Application.CourseSchedules.Commands.CancelCourse;

public record CancelCourseCommand : IRequest<CourseScheduleDetailDto?>
{
    public long CourseScheduleId { get; init; }
    public string Reason { get; init; } = "";
}
