using MediatR;

namespace Bagile.Application.Templates.Commands.SendFollowUpEmail;

public record SendFollowUpTestEmailCommand : IRequest<SendFollowUpTestEmailResult>
{
    public long CourseScheduleId { get; init; }

    /// <summary>
    /// Override the course type used to look up the template.
    /// If null, the course code prefix is used (e.g. "PSPO-300326-CB" → "PSPO").
    /// </summary>
    public string? CourseTypeOverride { get; init; }

    /// <summary>
    /// Explicit recipient email. If null, the trainer email is derived from TrainerName.
    /// </summary>
    public string? RecipientEmail { get; init; }
}

public record SendFollowUpTestEmailResult
{
    public string RecipientEmail { get; init; } = "";
    public string Subject { get; init; } = "";
    public string CourseType { get; init; } = "";
}
