using Bagile.Application.Common.Interfaces;
using Bagile.Application.Organisations.DTOs;
using MediatR;

namespace Bagile.Application.Organisations.Commands.UpdateOrgConfig;

public class UpdateOrgConfigCommandHandler : IRequestHandler<UpdateOrgConfigCommand, OrgConfigDto?>
{
    private readonly IOrganisationRepository _repo;

    public UpdateOrgConfigCommandHandler(IOrganisationRepository repo)
    {
        _repo = repo;
    }

    public Task<OrgConfigDto?> Handle(UpdateOrgConfigCommand request, CancellationToken ct)
        => _repo.UpdateConfigAsync(request.Id, request.Aliases, request.PrimaryDomain, ct);
}
