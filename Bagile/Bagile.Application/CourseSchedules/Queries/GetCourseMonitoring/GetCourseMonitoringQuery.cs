using Bagile.Application.CourseSchedules.DTOs;
using MediatR;

namespace Bagile.Application.CourseSchedules.Queries.GetCourseMonitoring;

public record GetCourseMonitoringQuery : IRequest<IEnumerable<CourseMonitoringDto>>
{
    public int DaysAhead { get; init; } = 30;
}
