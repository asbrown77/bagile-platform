using Bagile.Application.Organisations.DTOs;
using MediatR;

namespace Bagile.Application.Organisations.Commands.CreateOrganisation;

/// <summary>
/// Create a new entry in the organisations table.
/// Used by the portal type-ahead when a client org doesn't exist yet.
/// </summary>
public record CreateOrganisationCommand : IRequest<OrganisationSummaryDto>
{
    public string Name { get; init; } = "";
    public string? Acronym { get; init; }
}
