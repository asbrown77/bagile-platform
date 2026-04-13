using Bagile.Application.Organisations.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface IOrganisationQueries
{
    /// <summary>
    /// Search the organisations table by name (case-insensitive, partial match).
    /// Also matches against aliases. Returns up to 10 results for type-ahead.
    /// </summary>
    Task<IEnumerable<OrganisationSummaryDto>> SearchOrganisationsAsync(
        string q,
        CancellationToken ct = default);

    /// <summary>
    /// Get paginated list of organisations (derived from billing_company and email domains)
    /// </summary>
    Task<IEnumerable<OrganisationDto>> GetOrganisationsAsync(
        string? name,
        string? domain,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Count total organisations matching filters
    /// </summary>
    Task<int> CountOrganisationsAsync(
        string? name,
        string? domain,
        CancellationToken ct = default);

    /// <summary>
    /// Get detailed information for a single organisation
    /// </summary>
    Task<OrganisationDetailDto?> GetOrganisationByNameAsync(
        string name,
        int? year = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get course history for an organisation
    /// </summary>
    Task<IEnumerable<OrganisationCourseHistoryDto>> GetOrganisationCourseHistoryAsync(
        string organisationName,
        int? year = null,
        CancellationToken ct = default);
}