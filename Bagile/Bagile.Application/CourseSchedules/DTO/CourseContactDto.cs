namespace Bagile.Application.CourseSchedules.DTOs;

public record CourseContactDto
{
    public long Id { get; init; }
    public long CourseScheduleId { get; init; }
    public string Role { get; init; } = "";
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public DateTime CreatedAt { get; init; }
}
