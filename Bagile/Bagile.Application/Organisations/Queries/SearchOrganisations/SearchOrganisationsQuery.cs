using Bagile.Application.Organisations.DTOs;
using MediatR;

namespace Bagile.Application.Organisations.Queries.SearchOrganisations;

/// <summary>
/// Full-text search against the organisations table (name + aliases).
/// Returns up to 10 results for type-ahead use. Hits the organisations table directly,
/// unlike GetOrganisationsQuery which derives orgs from billing_company/student.company.
/// </summary>
public record SearchOrganisationsQuery : IRequest<IEnumerable<OrganisationSummaryDto>>
{
    public string Q { get; init; } = "";
}
