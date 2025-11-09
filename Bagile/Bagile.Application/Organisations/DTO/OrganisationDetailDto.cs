namespace Bagile.Application.Organisations.DTOs;

/// <summary>
/// Detailed organisation information
/// </summary>
public record OrganisationDetailDto : OrganisationDto
{
    public int TotalOrders { get; init; }
    public decimal TotalRevenue { get; init; }
    public DateTime? FirstOrderDate { get; init; }
    public DateTime? LastOrderDate { get; init; }
    public DateTime? LastCourseDate { get; init; }
}