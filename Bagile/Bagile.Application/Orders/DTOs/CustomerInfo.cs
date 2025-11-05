namespace Bagile.Application.Orders.DTOs;

public record CustomerInfo
{
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Company { get; init; }
}