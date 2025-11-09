using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.CourseSchedules.Queries.GetCourseAttendees;

public class GetCourseAttendeesQueryHandler
    : IRequestHandler<GetCourseAttendeesQuery, IEnumerable<CourseAttendeeDto>>
{
    private readonly ICourseScheduleQueries _queries;
    private readonly ILogger<GetCourseAttendeesQueryHandler> _logger;

    public GetCourseAttendeesQueryHandler(
        ICourseScheduleQueries queries,
        ILogger<GetCourseAttendeesQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<IEnumerable<CourseAttendeeDto>> Handle(
        GetCourseAttendeesQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching attendees for course schedule: ScheduleId={ScheduleId}",
            request.ScheduleId);

        return await _queries.GetCourseAttendeesAsync(request.ScheduleId, ct);
    }
}