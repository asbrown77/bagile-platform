namespace Bagile.Application.PlannedCourses.DTOs;

public record CoursePublicationDto
{
    public int Id { get; init; }
    public string Gateway { get; init; } = "";
    public DateTime? PublishedAt { get; init; }
    public string? ExternalUrl { get; init; }
    public int? WoocommerceProductId { get; init; }
}
