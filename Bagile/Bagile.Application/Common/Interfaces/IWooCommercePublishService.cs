namespace Bagile.Application.Common.Interfaces;

/// <summary>
/// Creates WooCommerce products for planned courses.
/// Implemented in Infrastructure layer using WooCommerce REST API.
/// </summary>
public interface IWooCommercePublishService
{
    /// <summary>
    /// Creates a WooCommerce product by cloning the most recent product of the same course type
    /// and updating dates, SKU, trainer details, and FooEvents meta fields.
    /// </summary>
    /// <returns>Product ID and permalink URL, or null if creation failed.</returns>
    Task<WooPublishResult?> CreateProductAsync(WooPublishRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing WooCommerce product's meta_data (e.g., to add Scrum.org listing URL).
    /// </summary>
    Task<bool> UpdateProductMetaAsync(long productId, Dictionary<string, string> metaUpdates, CancellationToken ct = default);
}

public record WooPublishRequest
{
    /// <summary>Course type code, e.g., PSMA, PSPO, PSMAI</summary>
    public string CourseType { get; init; } = "";

    /// <summary>Course start date</summary>
    public DateTime StartDate { get; init; }

    /// <summary>Course end date</summary>
    public DateTime EndDate { get; init; }

    /// <summary>Trainer full name, e.g., "Alex Brown"</summary>
    public string TrainerName { get; init; } = "";

    /// <summary>Whether the course is virtual (Zoom) or face-to-face</summary>
    public bool IsVirtual { get; init; }

    /// <summary>Venue name (for F2F courses)</summary>
    public string? Venue { get; init; }
}

public record WooPublishResult
{
    public long ProductId { get; init; }
    public string ProductUrl { get; init; } = "";
}
