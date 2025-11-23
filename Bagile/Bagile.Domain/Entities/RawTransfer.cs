namespace Bagile.Domain.Entities;

public class RawTransfer
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long CourseScheduleId { get; set; }
    public string FromStudentEmail { get; set; } = "";
    public string ToStudentEmail { get; set; } = "";
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}