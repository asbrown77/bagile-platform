using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Students.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Students.Queries.GetStudentById;

public class GetStudentByIdQueryHandler
    : IRequestHandler<GetStudentByIdQuery, StudentDetailDto?>
{
    private readonly IStudentQueries _queries;
    private readonly ILogger<GetStudentByIdQueryHandler> _logger;

    public GetStudentByIdQueryHandler(
        IStudentQueries queries,
        ILogger<GetStudentByIdQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<StudentDetailDto?> Handle(
        GetStudentByIdQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation("Fetching student: StudentId={StudentId}", request.StudentId);

        return await _queries.GetStudentByIdAsync(request.StudentId, ct);
    }
}