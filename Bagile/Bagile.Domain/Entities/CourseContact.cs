namespace Bagile.Domain.Entities;

public class CourseContact
{
    public long Id { get; set; }
    public long CourseScheduleId { get; set; }

    /// <summary>Role of this contact: admin, organiser, or other.</summary>
    public string Role { get; set; } = "other";

    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}
