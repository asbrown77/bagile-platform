namespace Bagile.Domain.Entities;

public class PlannedCourse
{
    public int Id { get; set; }
    public string CourseType { get; set; } = "";
    public int TrainerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsVirtual { get; set; } = true;
    public string? Venue { get; set; }
    public string? Notes { get; set; }
    public DateTime? DecisionDeadline { get; set; }
    public bool IsPrivate { get; set; }
    public string Status { get; set; } = "planned";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
