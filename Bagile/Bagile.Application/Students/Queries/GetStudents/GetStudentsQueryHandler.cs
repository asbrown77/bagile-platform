using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Common.Models;
using Bagile.Application.Students.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Students.Queries.GetStudents;

public class GetStudentsQueryHandler
    : IRequestHandler<GetStudentsQuery, PagedResult<StudentDto>>
{
    private readonly IStudentQueries _queries;
    private readonly ILogger<GetStudentsQueryHandler> _logger;

    public GetStudentsQueryHandler(
        IStudentQueries queries,
        ILogger<GetStudentsQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<PagedResult<StudentDto>> Handle(
        GetStudentsQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching students: Email={Email}, Name={Name}, Organisation={Organisation}, " +
            "CourseCode={CourseCode}, Page={Page}",
            request.Email, request.Name, request.Organisation,
            request.CourseCode, request.Page);

        var students = await _queries.GetStudentsAsync(
            request.Email,
            request.Name,
            request.Organisation,
            request.CourseCode,
            request.Page,
            request.PageSize,
            ct);

        var totalCount = await _queries.CountStudentsAsync(
            request.Email,
            request.Name,
            request.Organisation,
            request.CourseCode,
            ct);

        return new PagedResult<StudentDto>
        {
            Items = students,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}