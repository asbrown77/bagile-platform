using Bagile.Application.Common.Interfaces;
using Bagile.Application.Organisations.DTOs;
using MediatR;

namespace Bagile.Application.Organisations.Queries.GetOrgConfig;

public class GetOrgConfigQueryHandler : IRequestHandler<GetOrgConfigQuery, OrgConfigDto?>
{
    private readonly IOrganisationRepository _repo;

    public GetOrgConfigQueryHandler(IOrganisationRepository repo)
    {
        _repo = repo;
    }

    public Task<OrgConfigDto?> Handle(GetOrgConfigQuery request, CancellationToken ct)
        => _repo.GetConfigByNameAsync(request.Name, ct);
}
