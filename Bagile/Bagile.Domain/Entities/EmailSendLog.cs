namespace Bagile.Domain.Entities;

public class EmailSendLog
{
    public int Id { get; set; }
    public int? CourseScheduleId { get; set; }

    /// <summary>'pre_course' or 'post_course'</summary>
    public string TemplateType { get; set; } = "";

    public string? SentBy { get; set; }
    public int RecipientCount { get; set; }

    /// <summary>Comma-separated list of recipient email addresses.</summary>
    public string? Recipients { get; set; }

    public string? Subject { get; set; }
    public bool IsTest { get; set; }
    public DateTime SentAt { get; set; }
}
