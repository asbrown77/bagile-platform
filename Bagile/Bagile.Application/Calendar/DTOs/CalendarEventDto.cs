namespace Bagile.Application.Calendar.DTOs;

public record CalendarEventDto
{
    public string Id { get; init; } = "";                    // "planned-123" or "schedule-456"
    public string CourseType { get; init; } = "";
    public string? TrainerInitials { get; init; }
    public string? TrainerName { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsPrivate { get; init; }
    public string Status { get; init; } = "planned";        // planned | partial_live | live | cancelled
    public DateTime? DecisionDeadline { get; init; }
    public int EnrolmentCount { get; init; }
    public int MinimumEnrolments { get; init; } = 3;
    public string? Venue { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<GatewayStatusDto> Gateways { get; init; } = Array.Empty<GatewayStatusDto>();
}

public record GatewayStatusDto
{
    public string Type { get; init; } = "";
    public bool Published { get; init; }
    public string? Url { get; init; }
}
