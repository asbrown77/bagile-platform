using MediatR;
using Bagile.Application.CourseSchedules.DTOs;

namespace Bagile.Application.CourseSchedules.Queries.GetCourseScheduleById;

public record GetCourseScheduleByIdQuery(long ScheduleId)
    : IRequest<CourseScheduleDetailDto?>;