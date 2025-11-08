namespace Bagile.Application.Orders.DTOs;

public record EnrolmentDto
{
    public long EnrolmentId { get; init; }
    public string Status { get; init; } = "active";
    public string StudentEmail { get; init; } = "";
    public string StudentName { get; init; } = "";
    public string? CourseName { get; init; }
    public DateTime? CourseStartDate { get; init; }

    // Transfer tracking
    public bool IsTransfer { get; init; }
    public long? TransferredFromEnrolmentId { get; init; }
    public long? TransferredToEnrolmentId { get; init; }
    public string? OriginalSku { get; init; }
    public string? TransferReason { get; init; }
    public string? TransferReasonLabel { get; init; }
    public bool? RefundEligible { get; init; }
    public string? TransferNotes { get; init; }
}