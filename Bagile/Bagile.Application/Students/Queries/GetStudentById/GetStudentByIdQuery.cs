using MediatR;
using Bagile.Application.Students.DTOs;

namespace Bagile.Application.Students.Queries.GetStudentById;

public record GetStudentByIdQuery(long StudentId)
    : IRequest<StudentDetailDto?>;