using MediatR;
using Bagile.Application.Transfers.DTOs;

namespace Bagile.Application.Transfers.Queries.GetTransfersByCourse;

public record GetTransfersByCourseQuery(long ScheduleId)
    : IRequest<TransfersByCourseDto>;