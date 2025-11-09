namespace Bagile.Application.Students.DTOs;

/// <summary>
/// Detailed student information with computed stats
/// </summary>
public record StudentDetailDto : StudentDto
{
    public int TotalEnrolments { get; init; }
    public DateTime? LastCourseDate { get; init; }
    public DateTime UpdatedAt { get; init; }
}