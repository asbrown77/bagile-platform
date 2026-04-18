namespace Bagile.Application.PlannedCourses.DTOs;

public record EcommercePublishResultDto
{
    public long ProductId { get; init; }
    public string ProductUrl { get; init; } = "";
    public string Status { get; init; } = "created";
    /// <summary>Non-fatal issues detected during post-creation sanity check.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}

public record ScrumOrgPublishResultDto
{
    public string ListingUrl { get; init; } = "";
    public string Status { get; init; } = "created";
}
