namespace Bagile.Application.CourseSchedules.DTOs;

public record CourseAttendeeDto
{
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
}
