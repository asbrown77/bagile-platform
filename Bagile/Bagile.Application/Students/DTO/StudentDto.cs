namespace Bagile.Application.Students.DTOs;

/// <summary>
/// Student list item
/// </summary>
public record StudentDto
{
    public long Id { get; init; }
    public string Email { get; init; } = "";
    public string FirstName { get; init; } = "";
    public string LastName { get; init; } = "";
    public string FullName { get; init; } = "";
    public string? Company { get; init; }
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// True if any field has been manually overridden (overridden_fields is non-empty).
    /// Shown in portal as a badge so trainers know this record has been corrected.
    /// </summary>
    public bool IsOverridden { get; init; }

    public string? UpdatedBy { get; init; }
    public string? OverrideNote { get; init; }
}