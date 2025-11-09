using MediatR;
using Bagile.Application.Common.Models;
using Bagile.Application.Transfers.DTOs;

namespace Bagile.Application.Transfers.Queries.GetTransfers;

public record GetTransfersQuery : IRequest<PagedResult<TransferDto>>
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string? Reason { get; init; }                // CourseCancelled, StudentRequest, Other
    public string? OrganisationName { get; init; }
    public long? CourseScheduleId { get; init; }        // Either source or target
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}