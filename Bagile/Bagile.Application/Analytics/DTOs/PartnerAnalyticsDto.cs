namespace Bagile.Application.Analytics.DTOs;

public record PartnerAnalyticsDto
{
    public string Name { get; init; } = "";
    public string? PtnTier { get; init; }
    public decimal? DiscountRate { get; init; }
    public string? ContactEmail { get; init; }
    public int BookingsThisYear { get; init; }
    public int DelegatesThisYear { get; init; }
    public decimal SpendThisYear { get; init; }
    public string CalculatedTier { get; init; } = "";
    public int CalculatedDiscount { get; init; }
    public bool TierMismatch { get; init; }
}
