namespace Bagile.Application.Analytics.DTOs;

public record RepeatCustomerDto
{
    public string Company { get; init; } = "";
    public int TotalBookings { get; init; }
    public decimal LifetimeSpend { get; init; }
    public int LifetimeDelegates { get; init; }
    public DateTime? FirstBooking { get; init; }
    public DateTime? LastBooking { get; init; }
    public int RelationshipDays { get; init; }
    public int BookingsThisYear { get; init; }
    public decimal SpendThisYear { get; init; }
}
