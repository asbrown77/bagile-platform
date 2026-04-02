using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Templates.Commands.SendFollowUpEmail;

public class SendFollowUpTestEmailCommandHandler
    : IRequestHandler<SendFollowUpTestEmailCommand, SendFollowUpTestEmailResult>
{
    // Known trainer name → email mappings.
    // Extend here if additional trainers are added.
    private static readonly Dictionary<string, string> TrainerEmails =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Alex Brown"]  = "alexbrown@bagile.co.uk",
            ["Chris Bexon"] = "chrisbexon@bagile.co.uk",
        };

    private readonly ICourseScheduleQueries _scheduleQueries;
    private readonly IPostCourseTemplateRepository _templateRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendFollowUpTestEmailCommandHandler> _logger;

    public SendFollowUpTestEmailCommandHandler(
        ICourseScheduleQueries scheduleQueries,
        IPostCourseTemplateRepository templateRepo,
        IEmailService emailService,
        ILogger<SendFollowUpTestEmailCommandHandler> logger)
    {
        _scheduleQueries = scheduleQueries;
        _templateRepo    = templateRepo;
        _emailService    = emailService;
        _logger          = logger;
    }

    public async Task<SendFollowUpTestEmailResult> Handle(
        SendFollowUpTestEmailCommand request, CancellationToken ct)
    {
        // 1. Load course schedule
        var course = await _scheduleQueries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct)
            ?? throw new KeyNotFoundException($"Course schedule {request.CourseScheduleId} not found.");

        // 2. Determine course type
        var courseType = request.CourseTypeOverride?.ToUpper()
            ?? DeriveCourseType(course.CourseCode);

        // 3. Load template
        var template = await _templateRepo.GetByCourseTypeAsync(courseType, ct)
            ?? throw new InvalidOperationException(
                $"No post-course template found for course type '{courseType}'. " +
                $"Create one via PUT /api/templates/post-course/{courseType}.");

        // 4. Resolve recipient — explicit override, then trainer lookup, then fallback
        var recipientEmail = ResolveRecipient(request.RecipientEmail, course.TrainerName);

        // 5. Build variable map with placeholder attendee data so the template renders sensibly
        var courseDates = BuildCourseDates(course.StartDate, course.EndDate);
        var trainerName = course.TrainerName ?? "Alex and Chris";

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["greeting"]     = "Hi [TEST]",
            ["trainer_name"] = trainerName,
            ["course_dates"] = courseDates,
            ["delay_note"]   = "",
            ["course_title"] = course.Title,
            ["course_code"]  = course.CourseCode,
            ["course_type"]  = courseType,
        };

        var subject  = $"[TEST] {ApplyVariables(template.SubjectTemplate, variables)}";
        var htmlBody = ApplyVariables(template.HtmlBody, variables);

        // 6. Send to single test recipient (no CC)
        _logger.LogInformation(
            "Sending test follow-up email for course {CourseId} ({CourseCode}) to {Recipient}",
            request.CourseScheduleId, course.CourseCode, recipientEmail);

        await _emailService.SendAsync(
            to:       [recipientEmail],
            subject:  subject,
            htmlBody: htmlBody,
            cc:       [],
            ct:       ct);

        return new SendFollowUpTestEmailResult
        {
            RecipientEmail = recipientEmail,
            Subject        = subject,
            CourseType     = courseType,
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static string ResolveRecipient(string? explicitEmail, string? trainerName)
    {
        if (!string.IsNullOrWhiteSpace(explicitEmail))
            return explicitEmail.Trim();

        if (trainerName is not null && TrainerEmails.TryGetValue(trainerName.Trim(), out var mapped))
            return mapped;

        // Fallback — always reaches a real inbox
        return "alexbrown@bagile.co.uk";
    }

    private static string DeriveCourseType(string courseCode)
    {
        var prefix = courseCode.Split('-')[0];
        return prefix.ToUpper();
    }

    private static string BuildCourseDates(DateTime? start, DateTime? end)
    {
        if (start is null) return "";
        var startStr = start.Value.ToString("d MMMM yyyy");
        if (end is null || end.Value.Date == start.Value.Date) return startStr;
        return $"{startStr} – {end.Value:d MMMM yyyy}";
    }

    private static string ApplyVariables(string template, Dictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        return template;
    }
}
