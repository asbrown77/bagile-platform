namespace Bagile.Application.Organisations.DTOs;

public record OrgConfigDto
{
    public long Id { get; init; }
    public string Name { get; init; } = "";
    public List<string> Aliases { get; init; } = new();
    public string? PrimaryDomain { get; init; }
    public string? PartnerType { get; init; }
    public string? PtnTier { get; init; }
    public decimal? DiscountRate { get; init; }
    public string? ContactEmail { get; init; }
}
