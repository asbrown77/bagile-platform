using Bagile.Application.Organisations.DTOs;

namespace Bagile.Application.Common.Interfaces;

/// <summary>
/// Write-side interface for the organisations table.
/// Read-side queries live in IOrganisationQueries.
/// </summary>
public interface IOrganisationRepository
{
    /// <summary>
    /// Insert a new organisation. Returns the created summary including the generated id.
    /// Throws if a duplicate name already exists.
    /// </summary>
    Task<OrganisationSummaryDto> CreateAsync(string name, string? acronym, CancellationToken ct = default);
}
