using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Transfers.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Transfers.Queries.GetPendingTransfers;

public class GetPendingTransfersQueryHandler
    : IRequestHandler<GetPendingTransfersQuery, IEnumerable<PendingTransferDto>>
{
    private readonly ITransferQueries _queries;
    private readonly ILogger<GetPendingTransfersQueryHandler> _logger;

    public GetPendingTransfersQueryHandler(
        ITransferQueries queries,
        ILogger<GetPendingTransfersQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<IEnumerable<PendingTransferDto>> Handle(
        GetPendingTransfersQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation("Fetching pending transfers (cancelled but not rebooked)");

        return await _queries.GetPendingTransfersAsync(ct);
    }
}