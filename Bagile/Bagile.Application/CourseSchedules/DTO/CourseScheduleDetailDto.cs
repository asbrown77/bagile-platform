namespace Bagile.Application.CourseSchedules.DTOs;

/// <summary>
/// Detailed course schedule information
/// </summary>
public record CourseScheduleDetailDto : CourseScheduleDto
{
    public int? Capacity { get; init; }
    public decimal? Price { get; init; }
    public string? Sku { get; init; }
    public string? TrainerName { get; init; }
    public string? FormatType { get; init; }        // virtual/in_person
    public string? SourceSystem { get; init; }
    public long? SourceProductId { get; init; }
    public DateTime? LastSynced { get; init; }
}