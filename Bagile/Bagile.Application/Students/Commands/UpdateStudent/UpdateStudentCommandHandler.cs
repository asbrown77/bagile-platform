using Bagile.Application.Students.DTOs;
using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Students.Commands.UpdateStudent;

public class UpdateStudentCommandHandler
    : IRequestHandler<UpdateStudentCommand, StudentDetailDto?>
{
    private readonly IStudentRepository _studentRepo;
    private readonly IStudentQueries _queries;
    private readonly ILogger<UpdateStudentCommandHandler> _logger;

    public UpdateStudentCommandHandler(
        IStudentRepository studentRepo,
        IStudentQueries queries,
        ILogger<UpdateStudentCommandHandler> logger)
    {
        _studentRepo = studentRepo;
        _queries     = queries;
        _logger      = logger;
    }

    public async Task<StudentDetailDto?> Handle(UpdateStudentCommand request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Overriding student {StudentId} fields by {UpdatedBy}",
            request.Id, request.UpdatedBy ?? "portal");

        var @override = new StudentOverride
        {
            Email        = request.Email,
            FirstName    = request.FirstName,
            LastName     = request.LastName,
            Company      = request.Company,
            UpdatedBy    = request.UpdatedBy ?? "portal",
            OverrideNote = request.OverrideNote
        };

        var updated = await _studentRepo.OverrideAsync(request.Id, @override, ct);
        if (updated is null) return null;

        // Return the full enriched DTO (includes enrolment count, last course date)
        return await _queries.GetStudentByIdAsync(request.Id, ct);
    }
}
