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
    /// Optional note to inject as the {{delay_note}} variable.
    /// Example: "Apologies for the delay in getting these over to you."
    /// </summary>
    public string? DelayNote { get; init; }
}

public record SendFollowUpEmailResult
{
    public int RecipientCount { get; init; }
    public string Subject { get; init; } = "";
    public string CourseType { get; init; } = "";
    public IReadOnlyList<string> RecipientEmails { get; init; } = [];
}
