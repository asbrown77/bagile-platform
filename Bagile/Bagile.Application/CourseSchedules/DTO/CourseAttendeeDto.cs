namespace Bagile.Application.CourseSchedules.DTOs;

public record CourseAttendeeDto
{
    public long EnrolmentId { get; init; }
    public long StudentId { get; init; }
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Organisation { get; init; }
    public string Status { get; init; } = "";
    public string? CourseCode { get; init; }
    public string? CourseName { get; init; }
    public string? Country { get; init; }
    public string? OrderNumber { get; init; }
    public decimal? OrderAmount { get; init; }
    public string? OrderStatus { get; init; }
    public string? Currency { get; init; }
}
