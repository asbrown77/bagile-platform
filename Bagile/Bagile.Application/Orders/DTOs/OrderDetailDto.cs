namespace Bagile.Application.Orders.DTOs;

public record OrderDetailDto : OrderDto
{
    public CustomerInfo Customer { get; init; } = new();
    public IEnumerable<LineItemDto> LineItems { get; init; } = Enumerable.Empty<LineItemDto>();
    public IEnumerable<EnrolmentDto> Enrolments { get; init; } = Enumerable.Empty<EnrolmentDto>();
}