using MediatR;

namespace Bagile.Application.Templates.Commands.SendPreCourseEmail;

public record SendPreCourseEmailCommand : IRequest<SendPreCourseEmailResult>
{
    public long CourseScheduleId { get; init; }

    /// <summary>
    /// Override the format used to look up the template.
    /// If null, derived from the course's FormatType ('virtual' or 'f2f').
    /// Allowed values: 'virtual', 'f2f'.
    /// </summary>
    public string? FormatOverride { get; init; }

    /// <summary>
    /// If provided, used as the email body instead of the stored template.
    /// This supports the compose flow where the trainer edits before sending.
    /// </summary>
    public string? HtmlBodyOverride { get; init; }
}

public record SendPreCourseEmailResult
{
    public int RecipientCount { get; init; }
    public string Subject { get; init; } = "";
    public string CourseType { get; init; } = "";
    public string Format { get; init; } = "";
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
}

public record SendPreCourseTestEmailCommand : IRequest<SendPreCourseTestEmailResult>
{
    public long CourseScheduleId { get; init; }
    public string? FormatOverride { get; init; }
    public string? HtmlBodyOverride { get; init; }

    /// <summary>
    /// Override the test recipient email.
    /// If omitted, derived from the course's trainer via the trainers table.
    /// </summary>
    public string? RecipientEmail { get; init; }
}

public record SendPreCourseTestEmailResult
{
    public string RecipientEmail { get; init; } = "";
    public string Subject { get; init; } = "";
    public string CourseType { get; init; } = "";
    public string Format { get; init; } = "";
}
