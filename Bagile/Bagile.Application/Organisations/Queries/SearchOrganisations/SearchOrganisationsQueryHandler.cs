using Bagile.Application.Common.Interfaces;
using Bagile.Application.Organisations.DTOs;
using MediatR;

namespace Bagile.Application.Organisations.Queries.SearchOrganisations;

public class SearchOrganisationsQueryHandler
    : IRequestHandler<SearchOrganisationsQuery, IEnumerable<OrganisationSummaryDto>>
{
    private readonly IOrganisationQueries _queries;

    public SearchOrganisationsQueryHandler(IOrganisationQueries queries)
    {
        _queries = queries;
    }

    public Task<IEnumerable<OrganisationSummaryDto>> Handle(
        SearchOrganisationsQuery request,
        CancellationToken ct)
    {
        return _queries.SearchOrganisationsAsync(request.Q, ct);
    }
}
