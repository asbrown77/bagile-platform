namespace Bagile.Application.Common.Models;

/// <summary>
/// Reusable date range filter for queries
/// </summary>
public record DateRangeFilter
{
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
}