using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Common.Models;
using Bagile.Application.Transfers.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Transfers.Queries.GetTransfers;

public class GetTransfersQueryHandler
    : IRequestHandler<GetTransfersQuery, PagedResult<TransferDto>>
{
    private readonly ITransferQueries _queries;
    private readonly ILogger<GetTransfersQueryHandler> _logger;

    public GetTransfersQueryHandler(
        ITransferQueries queries,
        ILogger<GetTransfersQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<PagedResult<TransferDto>> Handle(
        GetTransfersQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching transfers: From={From}, To={To}, Reason={Reason}, " +
            "Organisation={Organisation}, CourseScheduleId={CourseScheduleId}, Page={Page}",
            request.From, request.To, request.Reason,
            request.OrganisationName, request.CourseScheduleId, request.Page);

        var transfers = await _queries.GetTransfersAsync(
            request.From,
            request.To,
            request.Reason,
            request.OrganisationName,
            request.CourseScheduleId,
            request.Page,
            request.PageSize,
            ct);

        var totalCount = await _queries.CountTransfersAsync(
            request.From,
            request.To,
            request.Reason,
            request.OrganisationName,
            request.CourseScheduleId,
            ct);

        return new PagedResult<TransferDto>
        {
            Items = transfers,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}