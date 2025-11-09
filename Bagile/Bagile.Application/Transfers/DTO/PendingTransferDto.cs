namespace Bagile.Application.Transfers.DTOs;

/// <summary>
/// Student whose original course was cancelled but hasn't been rebooked
/// </summary>
public record PendingTransferDto
{
    public long StudentId { get; init; }
    public string StudentName { get; init; } = "";
    public string StudentEmail { get; init; } = "";
    public string? Organisation { get; init; }

    public long CancelledScheduleId { get; init; }
    public string CourseCode { get; init; } = "";
    public string CourseTitle { get; init; } = "";
    public DateTime? OriginalStartDate { get; init; }
    public DateTime CancelledDate { get; init; }

    public int DaysSinceCancellation { get; init; }
}