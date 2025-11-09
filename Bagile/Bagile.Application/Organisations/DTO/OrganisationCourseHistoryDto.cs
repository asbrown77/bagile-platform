namespace Bagile.Application.Organisations.DTOs;

/// <summary>
/// Course history for an organisation
/// </summary>
public record OrganisationCourseHistoryDto
{
    public string CourseCode { get; init; } = "";
    public string CourseTitle { get; init; } = "";
    public int PublicCount { get; init; }
    public int PrivateCount { get; init; }
    public int TotalCount { get; init; }
    public DateTime? LastRunDate { get; init; }
}