using MediatR;
using Bagile.Application.Common.Models;
using Bagile.Application.Enrolments.DTOs;

namespace Bagile.Application.Enrolments.Queries.GetEnrolments;

public record GetEnrolmentsQuery : IRequest<PagedResult<EnrolmentListDto>>
{
    public long? CourseScheduleId { get; init; }
    public long? StudentId { get; init; }
    public string? Status { get; init; }            // Booked, Completed, Cancelled, Transferred, NoShow
    public string? Organisation { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}