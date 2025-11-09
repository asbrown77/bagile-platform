namespace Bagile.Application.Transfers.DTOs;

/// <summary>
/// Transfer record showing student movement between course schedules
/// </summary>
public record TransferDto
{
    public long StudentId { get; init; }
    public string StudentName { get; init; } = "";
    public string StudentEmail { get; init; } = "";
    public string? Organisation { get; init; }

    public long FromScheduleId { get; init; }
    public string FromCourseCode { get; init; } = "";
    public string FromCourseTitle { get; init; } = "";
    public DateTime? FromCourseStartDate { get; init; }
    public string? FromCourseStatus { get; init; }

    public long ToScheduleId { get; init; }
    public string ToCourseCode { get; init; } = "";
    public string ToCourseTitle { get; init; } = "";
    public DateTime? ToCourseStartDate { get; init; }
    public string? ToCourseStatus { get; init; }

    public string Reason { get; init; } = "";           // CourseCancelled, StudentRequest, Other
    public DateTime TransferDate { get; init; }
}