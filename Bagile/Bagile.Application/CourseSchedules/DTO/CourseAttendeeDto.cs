namespace Bagile.Application.CourseSchedules.DTOs;

/// <summary>
/// Student enrolled in a course schedule
/// </summary>
public record CourseAttendeeDto
{
    public long StudentId { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Organisation { get; init; }
    public string Status { get; init; } = "";       // Booked, Completed, Cancelled, Transferred
}