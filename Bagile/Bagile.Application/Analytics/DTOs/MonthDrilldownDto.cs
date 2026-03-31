namespace Bagile.Application.Analytics.DTOs;

public record MonthDrilldownDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string MonthName { get; init; } = "";
    public decimal TotalRevenue { get; init; }
    public int TotalOrders { get; init; }
    public IEnumerable<MonthlyRevenueDetailDto> Orders { get; init; } = [];
}

public record MonthlyRevenueDetailDto
{
    public long OrderId { get; init; }
    public string ExternalId { get; init; } = "";
    public DateTime? OrderDate { get; init; }
    public string? Company { get; init; }
    public string? ContactName { get; init; }
    public string? ContactEmail { get; init; }
    public decimal NetRevenue { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal RefundAmount { get; init; }
    public string? LifecycleStatus { get; init; }
    public string? PaymentMethod { get; init; }
    public string? CourseCode { get; init; }
    public string? CourseName { get; init; }
    public DateTime? CourseDate { get; init; }
    public string? CourseType { get; init; }
    public int AttendeeCount { get; init; }
}
