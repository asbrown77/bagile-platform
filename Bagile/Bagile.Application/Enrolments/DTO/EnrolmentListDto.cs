namespace Bagile.Application.Enrolments.DTOs;

/// <summary>
/// Enrolment list item with related course and student info
/// </summary>
public record EnrolmentListDto
{
    public long Id { get; init; }
    public long CourseScheduleId { get; init; }
    public string CourseCode { get; init; } = "";
    public string CourseTitle { get; init; } = "";
    public DateTime? CourseStartDate { get; init; }
    public long StudentId { get; init; }
    public string StudentName { get; init; } = "";
    public string StudentEmail { get; init; } = "";
    public string? Organisation { get; init; }
    public string Status { get; init; } = "Booked";
    public bool IsTransfer { get; init; }
    public long? TransferFromScheduleId { get; init; }
    public long? TransferToScheduleId { get; init; }
    public DateTime CreatedAt { get; init; }
}