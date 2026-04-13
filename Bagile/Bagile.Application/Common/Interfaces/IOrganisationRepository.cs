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

    /// <summary>
    /// Get full configuration for an organisation, matched by name or alias.
    /// Returns null if not found in the organisations table.
    /// </summary>
    Task<OrgConfigDto?> GetConfigByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Update aliases and primary domain for an organisation. Returns the updated record.
    /// </summary>
    Task<OrgConfigDto?> UpdateConfigAsync(long id, List<string> aliases, string? primaryDomain, CancellationToken ct = default);
}
