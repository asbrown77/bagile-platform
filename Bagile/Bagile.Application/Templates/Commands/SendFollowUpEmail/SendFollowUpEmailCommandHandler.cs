using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Templates.Commands.SendFollowUpEmail;

public class SendFollowUpEmailCommandHandler
    : IRequestHandler<SendFollowUpEmailCommand, SendFollowUpEmailResult>
{
    private const string CcAddress = "info@bagile.co.uk";

    private readonly ICourseScheduleQueries _scheduleQueries;
    private readonly IPostCourseTemplateRepository _templateRepo;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendFollowUpEmailCommandHandler> _logger;

    public SendFollowUpEmailCommandHandler(
        ICourseScheduleQueries scheduleQueries,
        IPostCourseTemplateRepository templateRepo,
        IEmailService emailService,
        ILogger<SendFollowUpEmailCommandHandler> logger)
    {
        _scheduleQueries = scheduleQueries;
        _templateRepo    = templateRepo;
        _emailService    = emailService;
        _logger          = logger;
    }

    public async Task<SendFollowUpEmailResult> Handle(
        SendFollowUpEmailCommand request, CancellationToken ct)
    {
        // 1. Load course schedule
        var course = await _scheduleQueries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct)
            ?? throw new KeyNotFoundException($"Course schedule {request.CourseScheduleId} not found.");

        // 2. Determine course type from code prefix or override
        var courseType = request.CourseTypeOverride?.ToUpper()
            ?? DeriveCourseType(course.CourseCode);

        // 3. Load template
        var template = await _templateRepo.GetByCourseTypeAsync(courseType, ct)
            ?? throw new InvalidOperationException(
                $"No post-course template found for course type '{courseType}'. " +
                $"Create one via PUT /api/templates/post-course/{courseType}.");

        // 4. Load active attendees
        var attendees = (await _scheduleQueries.GetCourseAttendeesAsync(request.CourseScheduleId, ct))
            .Where(a => a.Status == "active")
            .ToList();

        if (attendees.Count == 0)
            throw new InvalidOperationException(
                $"Course {request.CourseScheduleId} has no active attendees — email not sent.");

        var toEmails = attendees.Select(a => a.Email).Distinct().ToList();

        // 5. Build variable map and populate template
        var courseDates = BuildCourseDates(course.StartDate, course.EndDate);
        var trainerName = course.TrainerName ?? "Alex and Chris";
        var greeting    = BuildGreeting(attendees.Select(a => a.FirstName).ToList());
        var delayNote   = string.IsNullOrWhiteSpace(request.DelayNote)
            ? ""
            : $"<p>{System.Net.WebUtility.HtmlEncode(request.DelayNote)}</p>";

        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["greeting"]     = greeting,
            ["trainer_name"] = trainerName,
            ["course_dates"] = courseDates,
            ["delay_note"]   = delayNote,
            ["course_title"] = course.Title,
            ["course_code"]  = course.CourseCode,
            ["course_type"]  = courseType,
        };

        var subject  = ApplyVariables(template.SubjectTemplate, variables);
        var htmlBody = ApplyVariables(template.HtmlBody, variables);

        // 6. Send
        _logger.LogInformation(
            "Sending follow-up email for course {CourseId} ({CourseCode}) to {Count} recipients",
            request.CourseScheduleId, course.CourseCode, toEmails.Count);

        await _emailService.SendAsync(
            to:       toEmails,
            subject:  subject,
            htmlBody: htmlBody,
            cc:       [CcAddress],
            ct:       ct);

        return new SendFollowUpEmailResult
        {
            RecipientCount  = toEmails.Count,
            Subject         = subject,
            CourseType      = courseType,
            RecipientEmails = toEmails.AsReadOnly()
        };
    }

    // ── Helpers ──────────────────────────────────────────────

    private static string DeriveCourseType(string courseCode)
    {
        // e.g. "PSPO-300326-CB" → "PSPO", "PSPOA-300326" → "PSPOA"
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

    private static string BuildGreeting(IList<string> firstNames)
    {
        if (firstNames.Count == 0) return "Hi";
        if (firstNames.Count == 1) return $"Hi {firstNames[0]}";
        if (firstNames.Count <= 3) return $"Hi {string.Join(", ", firstNames)}";
        return "Hi all";
    }

    private static string ApplyVariables(string template, Dictionary<string, string> variables)
    {
        foreach (var (key, value) in variables)
            template = template.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        return template;
    }
}
