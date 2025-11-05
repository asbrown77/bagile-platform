namespace Bagile.Application.Orders.DTOs;

public record LineItemDto
{
    public long ProductId { get; init; }
    public string Name { get; init; } = "";
    public int Quantity { get; init; }
    public decimal Price { get; init; }
}