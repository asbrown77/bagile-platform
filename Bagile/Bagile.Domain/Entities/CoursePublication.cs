namespace Bagile.Domain.Entities;

public class CoursePublication
{
    public int Id { get; set; }
    public int? PlannedCourseId { get; set; }
    public long? CourseScheduleId { get; set; }
    public string Gateway { get; set; } = "";
    public DateTime? PublishedAt { get; set; }
    public string? ExternalUrl { get; set; }
    public int? WoocommerceProductId { get; set; }
    public DateTime CreatedAt { get; set; }
}
