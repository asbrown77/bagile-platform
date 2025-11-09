using MediatR;
using Bagile.Application.Common.Models;
using Bagile.Application.Organisations.DTOs;

namespace Bagile.Application.Organisations.Queries.GetOrganisations;

public record GetOrganisationsQuery : IRequest<PagedResult<OrganisationDto>>
{
    public string? Name { get; init; }
    public string? Domain { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}