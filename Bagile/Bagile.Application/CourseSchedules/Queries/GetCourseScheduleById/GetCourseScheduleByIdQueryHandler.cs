using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.CourseSchedules.Queries.GetCourseScheduleById;

public class GetCourseScheduleByIdQueryHandler
    : IRequestHandler<GetCourseScheduleByIdQuery, CourseScheduleDetailDto?>
{
    private readonly ICourseScheduleQueries _queries;
    private readonly ILogger<GetCourseScheduleByIdQueryHandler> _logger;

    public GetCourseScheduleByIdQueryHandler(
        ICourseScheduleQueries queries,
        ILogger<GetCourseScheduleByIdQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<CourseScheduleDetailDto?> Handle(
        GetCourseScheduleByIdQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation("Fetching course schedule: ScheduleId={ScheduleId}", request.ScheduleId);

        return await _queries.GetCourseScheduleByIdAsync(request.ScheduleId, ct);
    }
}