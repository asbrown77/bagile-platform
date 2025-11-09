using MediatR;
using Bagile.Application.Common.Models;
using Bagile.Application.Students.DTOs;

namespace Bagile.Application.Students.Queries.GetStudents;

public record GetStudentsQuery : IRequest<PagedResult<StudentDto>>
{
    public string? Email { get; init; }
    public string? Name { get; init; }
    public string? Organisation { get; init; }
    public string? CourseCode { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}