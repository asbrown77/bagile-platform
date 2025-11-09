using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Common.Models;
using Bagile.Application.Enrolments.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Enrolments.Queries.GetEnrolments;

public class GetEnrolmentsQueryHandler
    : IRequestHandler<GetEnrolmentsQuery, PagedResult<EnrolmentListDto>>
{
    private readonly IEnrolmentQueries _queries;
    private readonly ILogger<GetEnrolmentsQueryHandler> _logger;

    public GetEnrolmentsQueryHandler(
        IEnrolmentQueries queries,
        ILogger<GetEnrolmentsQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<PagedResult<EnrolmentListDto>> Handle(
        GetEnrolmentsQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching enrolments: CourseScheduleId={CourseScheduleId}, StudentId={StudentId}, " +
            "Status={Status}, Organisation={Organisation}, From={From}, To={To}, Page={Page}",
            request.CourseScheduleId, request.StudentId, request.Status,
            request.Organisation, request.From, request.To, request.Page);

        var enrolments = await _queries.GetEnrolmentsAsync(
            request.CourseScheduleId,
            request.StudentId,
            request.Status,
            request.Organisation,
            request.From,
            request.To,
            request.Page,
            request.PageSize,
            ct);

        var totalCount = await _queries.CountEnrolmentsAsync(
            request.CourseScheduleId,
            request.StudentId,
            request.Status,
            request.Organisation,
            request.From,
            request.To,
            ct);

        return new PagedResult<EnrolmentListDto>
        {
            Items = enrolments,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}