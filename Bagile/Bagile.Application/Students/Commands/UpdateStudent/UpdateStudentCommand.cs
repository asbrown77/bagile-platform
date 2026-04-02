using Bagile.Application.Students.DTOs;
using MediatR;

namespace Bagile.Application.Students.Commands.UpdateStudent;

public record UpdateStudentCommand : IRequest<StudentDetailDto?>
{
    public long Id { get; init; }

    /// <summary>Null = do not change this field.</summary>
    public string? Email { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Company { get; init; }

    /// <summary>Identity of the person making the change, for the audit trail.</summary>
    public string? UpdatedBy { get; init; }

    /// <summary>Optional reason for the override, stored on the student record.</summary>
    public string? OverrideNote { get; init; }
}
