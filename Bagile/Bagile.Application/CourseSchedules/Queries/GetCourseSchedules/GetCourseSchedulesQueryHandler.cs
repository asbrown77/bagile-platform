using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Common.Models;
using Bagile.Application.CourseSchedules.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.CourseSchedules.Queries.GetCourseSchedules;

public class GetCourseSchedulesQueryHandler
    : IRequestHandler<GetCourseSchedulesQuery, PagedResult<CourseScheduleDto>>
{
    private readonly ICourseScheduleQueries _queries;
    private readonly ILogger<GetCourseSchedulesQueryHandler> _logger;

    public GetCourseSchedulesQueryHandler(
        ICourseScheduleQueries queries,
        ILogger<GetCourseSchedulesQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<PagedResult<CourseScheduleDto>> Handle(
        GetCourseSchedulesQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching course schedules: From={From}, To={To}, CourseCode={CourseCode}, " +
            "Trainer={Trainer}, Type={Type}, Status={Status}, Page={Page}",
            request.From, request.To, request.CourseCode,
            request.Trainer, request.Type, request.Status, request.Page);

        var schedules = await _queries.GetCourseSchedulesAsync(
            request.From,
            request.To,
            request.CourseCode,
            request.Trainer,
            request.Type,
            request.Status,
            request.Page,
            request.PageSize,
            ct);

        var totalCount = await _queries.CountCourseSchedulesAsync(
            request.From,
            request.To,
            request.CourseCode,
            request.Trainer,
            request.Type,
            request.Status,
            ct);

        return new PagedResult<CourseScheduleDto>
        {
            Items = schedules,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}