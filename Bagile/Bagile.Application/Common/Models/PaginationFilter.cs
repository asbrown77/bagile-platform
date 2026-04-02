namespace Bagile.Application.Common.Models;

/// <summary>
/// Reusable pagination parameters for queries
/// </summary>
public record PaginationFilter
{
    private const int MaxPageSize = 100;

    public int Page { get; init; } = 1;

    private int _pageSize = 20;

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Min(Math.Max(value, 1), MaxPageSize);
    }

    /// <summary>
    /// Calculates the OFFSET for SQL queries
    /// </summary>
    public int Offset => (Page - 1) * PageSize;
}