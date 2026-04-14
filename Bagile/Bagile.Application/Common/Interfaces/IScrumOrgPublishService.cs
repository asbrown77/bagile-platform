namespace Bagile.Application.Common.Interfaces;

/// <summary>
/// Creates Scrum.org course listings via browser automation.
/// Implemented in Infrastructure layer using Playwright.
/// </summary>
public interface IScrumOrgPublishService
{
    /// <summary>
    /// Copies the most recent course of the same type for the same trainer on Scrum.org,
    /// updates dates and registration URL, and saves the listing.
    /// </summary>
    /// <returns>The Scrum.org listing URL, or null if creation failed.</returns>
    Task<ScrumOrgPublishResult?> CreateListingAsync(ScrumOrgPublishRequest request, CancellationToken ct = default);
}

public record ScrumOrgPublishRequest
{
    /// <summary>Course type code, e.g., PSMA, PSPO, PSMAI</summary>
    public string CourseType { get; init; } = "";

    /// <summary>Course start date</summary>
    public DateTime StartDate { get; init; }

    /// <summary>Course end date</summary>
    public DateTime EndDate { get; init; }

    /// <summary>Trainer full name, e.g., "Alex Brown"</summary>
    public string TrainerName { get; init; } = "";

    /// <summary>WooCommerce product URL for the registration link</summary>
    public string RegistrationUrl { get; init; } = "";
}

public record ScrumOrgPublishResult
{
    public string ListingUrl { get; init; } = "";
}
