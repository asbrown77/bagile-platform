using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Students.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Students.Queries.GetStudentEnrolments;

public class GetStudentEnrolmentsQueryHandler
    : IRequestHandler<GetStudentEnrolmentsQuery, IEnumerable<StudentEnrolmentDto>>
{
    private readonly IStudentQueries _queries;
    private readonly ILogger<GetStudentEnrolmentsQueryHandler> _logger;

    public GetStudentEnrolmentsQueryHandler(
        IStudentQueries queries,
        ILogger<GetStudentEnrolmentsQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<IEnumerable<StudentEnrolmentDto>> Handle(
        GetStudentEnrolmentsQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching enrolments for student: StudentId={StudentId}",
            request.StudentId);

        return await _queries.GetStudentEnrolmentsAsync(request.StudentId, ct);
    }
}