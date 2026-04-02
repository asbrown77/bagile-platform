namespace Bagile.Application.Trainers;

public record TrainerDto
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Email { get; init; } = "";
    public string? Phone { get; init; }
    public bool IsActive { get; init; }
}
