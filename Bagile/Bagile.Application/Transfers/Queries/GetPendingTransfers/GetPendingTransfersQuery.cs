using MediatR;
using Bagile.Application.Transfers.DTOs;

namespace Bagile.Application.Transfers.Queries.GetPendingTransfers;

public record GetPendingTransfersQuery : IRequest<IEnumerable<PendingTransferDto>>;