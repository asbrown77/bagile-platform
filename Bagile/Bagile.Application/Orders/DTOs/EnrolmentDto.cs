namespace Bagile.Application.Orders.DTOs;

public record EnrolmentDto
{
    public long EnrolmentId { get; init; }
    public string StudentEmail { get; init; } = "";
    public string StudentName { get; init; } = "";
    public string? CourseName { get; init; }
    public DateTime? CourseStartDate { get; init; }
}