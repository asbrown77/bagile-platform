namespace Bagile.Domain.Entities;

public class Trainer
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public string? ScrumOrgProfileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
