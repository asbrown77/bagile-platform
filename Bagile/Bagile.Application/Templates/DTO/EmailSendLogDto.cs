namespace Bagile.Application.Templates.DTOs;

public record EmailSendLogDto
{
    public int Id { get; init; }
    public int? CourseScheduleId { get; init; }
    public string TemplateType { get; init; } = "";
    public string? SentBy { get; init; }
    public int RecipientCount { get; init; }
    public string? Recipients { get; init; }
    public string? Subject { get; init; }
    public bool IsTest { get; init; }
    public DateTime SentAt { get; init; }
}
