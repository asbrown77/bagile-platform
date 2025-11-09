using MediatR;
using Bagile.Application.Students.DTOs;

namespace Bagile.Application.Students.Queries.GetStudentEnrolments;

public record GetStudentEnrolmentsQuery(long StudentId)
    : IRequest<IEnumerable<StudentEnrolmentDto>>;