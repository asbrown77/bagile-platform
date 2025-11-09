using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Transfers.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Transfers.Queries.GetTransfersByCourse;

public class GetTransfersByCourseQueryHandler
    : IRequestHandler<GetTransfersByCourseQuery, TransfersByCourseDto>
{
    private readonly ITransferQueries _queries;
    private readonly ILogger<GetTransfersByCourseQueryHandler> _logger;

    public GetTransfersByCourseQueryHandler(
        ITransferQueries queries,
        ILogger<GetTransfersByCourseQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<TransfersByCourseDto> Handle(
        GetTransfersByCourseQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching transfers for course schedule: ScheduleId={ScheduleId}",
            request.ScheduleId);

        return await _queries.GetTransfersByCourseAsync(request.ScheduleId, ct);
    }
}