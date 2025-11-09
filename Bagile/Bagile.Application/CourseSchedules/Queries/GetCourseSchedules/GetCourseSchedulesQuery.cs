using MediatR;
using Bagile.Application.Common.Models;
using Bagile.Application.CourseSchedules.DTOs;

namespace Bagile.Application.CourseSchedules.Queries.GetCourseSchedules;

public record GetCourseSchedulesQuery : IRequest<PagedResult<CourseScheduleDto>>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? CourseCode { get; init; }
    public string? Trainer { get; init; }
    public string? Type { get; init; }              // public/private
    public string? Status { get; init; }            // published/draft/cancelled/completed
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}