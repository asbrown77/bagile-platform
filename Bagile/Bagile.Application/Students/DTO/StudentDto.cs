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
}