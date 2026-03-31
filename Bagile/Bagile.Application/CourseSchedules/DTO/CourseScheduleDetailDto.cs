namespace Bagile.Application.CourseSchedules.DTOs;

/// <summary>
/// Detailed course schedule information
/// </summary>
public record CourseScheduleDetailDto : CourseScheduleDto
{
    public decimal? Price { get; init; }
    public string? Sku { get; init; }
    public string? SourceSystem { get; init; }
    public long? SourceProductId { get; init; }
    public DateTime? LastSynced { get; init; }
    public string? InvoiceReference { get; init; }
    public string? MeetingUrl { get; init; }
    public string? MeetingId { get; init; }
    public string? MeetingPasscode { get; init; }
    public string? VenueAddress { get; init; }
    public string? Notes { get; init; }
}