using MediatR;

namespace Bagile.Application.Templates.Commands.SendFollowUpEmail;

public record SendFollowUpEmailCommand : IRequest<SendFollowUpEmailResult>
{
    public long CourseScheduleId { get; init; }

    /// <summary>
    /// Override the course type used to look up the template.
    /// If null, the course code prefix is used (e.g. "PSPO-300326-CB" → "PSPO").
    /// </summary>
    public string? CourseTypeOverride { get; init; }

    /// <summary>
    /// If provided, used as the email body instead of the stored template.
    /// Supports the compose flow where the trainer edits before sending.
    /// </summary>
    public string? HtmlBodyOverride { get; init; }

    /// <summary>Additional CC addresses, e.g. course organiser chosen in the portal.</summary>
    public IReadOnlyList<string> AdditionalCc { get; init; } = [];
}

public record SendFollowUpEmailResult
{
    public int RecipientCount { get; init; }
    public string Subject { get; init; } = "";
    public string CourseType { get; init; } = "";
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
}
