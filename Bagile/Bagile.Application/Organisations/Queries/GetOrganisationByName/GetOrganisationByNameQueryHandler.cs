using MediatR;
using Bagile.Application.Common.Interfaces;
using Bagile.Application.Organisations.DTOs;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Organisations.Queries.GetOrganisationByName;

public class GetOrganisationByNameQueryHandler
    : IRequestHandler<GetOrganisationByNameQuery, OrganisationDetailDto?>
{
    private readonly IOrganisationQueries _queries;
    private readonly ILogger<GetOrganisationByNameQueryHandler> _logger;

    public GetOrganisationByNameQueryHandler(
        IOrganisationQueries queries,
        ILogger<GetOrganisationByNameQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<OrganisationDetailDto?> Handle(
        GetOrganisationByNameQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation("Fetching organisation: Name={Name}", request.Name);

        return await _queries.GetOrganisationByNameAsync(request.Name, ct);
    }
}