namespace Bagile.Application.PlannedCourses.DTOs;

public record EcommercePublishResultDto
{
    public long ProductId { get; init; }
    public string ProductUrl { get; init; } = "";
    public string Status { get; init; } = "created";
}

public record ScrumOrgPublishResultDto
{
    public string ListingUrl { get; init; } = "";
    public string Status { get; init; } = "created";
}
