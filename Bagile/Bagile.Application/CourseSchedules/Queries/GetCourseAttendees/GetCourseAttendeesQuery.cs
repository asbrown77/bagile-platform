using MediatR;
using Bagile.Application.CourseSchedules.DTOs;

namespace Bagile.Application.CourseSchedules.Queries.GetCourseAttendees;

public record GetCourseAttendeesQuery(long ScheduleId, string? BillingCompany = null)
    : IRequest<IEnumerable<CourseAttendeeDto>>;