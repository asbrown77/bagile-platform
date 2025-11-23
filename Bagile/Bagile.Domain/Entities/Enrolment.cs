namespace Bagile.Domain.Entities
{
    public class Enrolment
    {
        public long Id { get; set; }
        public long StudentId { get; set; }
        public long OrderId { get; set; }
        public long? CourseScheduleId { get; set; }

        public string Status { get; set; } = "active";
        public long? TransferredToEnrolmentId { get; set; }
        public long? TransferredFromEnrolmentId { get; set; }
        public string? OriginalSku { get; set; }
        public string? TransferReason { get; set; }
        public string? TransferNotes { get; set; }
        public bool? RefundEligible { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime? CancelledAt { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public bool IsActive => Status == "active";
        public bool IsTransferred => Status == "transferred";
        public bool IsTransfer => !string.IsNullOrWhiteSpace(OriginalSku);
        public bool WasCourseCancelled => TransferReason == "course_cancelled";

    }
}