namespace Bagile.Application.Organisations.DTOs;

/// <summary>
/// Organisation list item (derived from billing_company and student companies)
/// </summary>
public record OrganisationDto
{
    public string Name { get; init; } = "";
    public string? PrimaryDomain { get; init; }
    public int TotalStudents { get; init; }
    public int TotalEnrolments { get; init; }
}