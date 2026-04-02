using Bagile.Application.Common.Interfaces;
using Bagile.Application.Organisations.DTOs;
using MediatR;

namespace Bagile.Application.Organisations.Commands.CreateOrganisation;

public class CreateOrganisationCommandHandler
    : IRequestHandler<CreateOrganisationCommand, OrganisationSummaryDto>
{
    private readonly IOrganisationRepository _repo;

    public CreateOrganisationCommandHandler(IOrganisationRepository repo)
    {
        _repo = repo;
    }

    public async Task<OrganisationSummaryDto> Handle(
        CreateOrganisationCommand request,
        CancellationToken ct)
    {
        return await _repo.CreateAsync(request.Name.Trim(), request.Acronym?.Trim(), ct);
    }
}
