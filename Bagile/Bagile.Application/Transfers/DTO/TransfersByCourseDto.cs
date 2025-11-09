namespace Bagile.Application.Transfers.DTOs;

/// <summary>
/// Transfer summary for a specific course schedule
/// </summary>
public record TransfersByCourseDto
{
    public long CourseScheduleId { get; init; }
    public string CourseCode { get; init; } = "";
    public string CourseTitle { get; init; } = "";
    public DateTime? StartDate { get; init; }
    public string Status { get; init; } = "";

    public IEnumerable<TransferOutDto> TransfersOut { get; init; } = Enumerable.Empty<TransferOutDto>();
    public IEnumerable<TransferInDto> TransfersIn { get; init; } = Enumerable.Empty<TransferInDto>();

    public int TotalTransfersOut { get; init; }
    public int TotalTransfersIn { get; init; }
}

public record TransferOutDto
{
    public long StudentId { get; init; }
    public string StudentName { get; init; } = "";
    public string StudentEmail { get; init; } = "";
    public long ToScheduleId { get; init; }
    public string ToCourseTitle { get; init; } = "";
    public DateTime? ToCourseStartDate { get; init; }
    public DateTime TransferDate { get; init; }
}

public record TransferInDto
{
    public long StudentId { get; init; }
    public string StudentName { get; init; } = "";
    public string StudentEmail { get; init; } = "";
    public long FromScheduleId { get; init; }
    public string FromCourseTitle { get; init; } = "";
    public DateTime? FromCourseStartDate { get; init; }
    public DateTime TransferDate { get; init; }
}