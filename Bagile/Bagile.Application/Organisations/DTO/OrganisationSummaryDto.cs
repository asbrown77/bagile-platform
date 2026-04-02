namespace Bagile.Application.Organisations.DTOs;

/// <summary>
/// Lightweight summary returned by the type-ahead search — records from the organisations table.
/// Distinct from OrganisationDto which is derived from billing_company/student.company.
/// </summary>
public record OrganisationSummaryDto
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public string? Acronym { get; init; }
    public string? PartnerType { get; init; }
    public string? PtnTier { get; init; }
}
