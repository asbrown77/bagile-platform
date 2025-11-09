namespace Bagile.Application.Students.DTOs;

/// <summary>
/// Student's enrolment timeline entry
/// </summary>
public record StudentEnrolmentDto
{
    public long EnrolmentId { get; init; }
    public long CourseScheduleId { get; init; }
    public string CourseCode { get; init; } = "";
    public string CourseTitle { get; init; } = "";
    public DateTime? CourseStartDate { get; init; }
    public string Status { get; init; } = "Booked";
    public string? Type { get; init; }              // public/private
    public DateTime EnrolledAt { get; init; }
}