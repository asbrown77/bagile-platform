using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Common.Models;
using Bagile.Application.Organisations.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Organisations.Queries.GetOrganisations;

public class GetOrganisationsQueryHandler
    : IRequestHandler<GetOrganisationsQuery, PagedResult<OrganisationDto>>
{
    private readonly IOrganisationQueries _queries;
    private readonly ILogger<GetOrganisationsQueryHandler> _logger;

    public GetOrganisationsQueryHandler(
        IOrganisationQueries queries,
        ILogger<GetOrganisationsQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<PagedResult<OrganisationDto>> Handle(
        GetOrganisationsQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "Fetching organisations: Name={Name}, Domain={Domain}, Page={Page}",
            request.Name, request.Domain, request.Page);

        var organisations = await _queries.GetOrganisationsAsync(
            request.Name,
            request.Domain,
            request.Page,
            request.PageSize,
            ct);

        var totalCount = await _queries.CountOrganisationsAsync(
            request.Name,
            request.Domain,
            ct);

        return new PagedResult<OrganisationDto>
        {
            Items = organisations,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
        };
    }
}