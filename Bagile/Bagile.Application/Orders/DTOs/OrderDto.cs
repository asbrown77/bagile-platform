namespace Bagile.Application.Orders.DTOs;

public record OrderDto
{
    public long Id { get; init; }
    public string Source { get; init; } = "";
    public string ExternalId { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Reference { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime? OrderDate { get; init; }
    public string? CustomerName { get; init; }
    public string? CustomerEmail { get; init; }
    public string? CustomerCompany { get; init; }
}