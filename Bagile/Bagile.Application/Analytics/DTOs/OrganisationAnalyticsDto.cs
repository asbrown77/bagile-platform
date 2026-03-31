namespace Bagile.Application.Analytics.DTOs;

public record OrganisationAnalyticsDto
{
    public string Company { get; init; } = "";
    public string? PartnerType { get; init; }
    public string? PtnTier { get; init; }
    public decimal? DiscountRate { get; init; }
    public int OrderCount { get; init; }
    public int DelegateCount { get; init; }
    public decimal TotalSpend { get; init; }
}

public record OrganisationAnalyticsResultDto
{
    public int Year { get; init; }
    public IEnumerable<OrganisationAnalyticsDto> Organisations { get; init; } = [];
}
