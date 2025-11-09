namespace Bagile.Application.Common.Models;

/// <summary>
/// Reusable pagination parameters for queries
/// </summary>
public record PaginationFilter
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// Calculates the OFFSET for SQL queries
    /// </summary>
    public int Offset => (Page - 1) * PageSize;
}