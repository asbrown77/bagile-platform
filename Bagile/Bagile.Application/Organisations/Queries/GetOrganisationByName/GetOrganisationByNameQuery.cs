using MediatR;
using Bagile.Application.Organisations.DTOs;

namespace Bagile.Application.Organisations.Queries.GetOrganisationByName;

public record GetOrganisationByNameQuery(string Name)
    : IRequest<OrganisationDetailDto?>;