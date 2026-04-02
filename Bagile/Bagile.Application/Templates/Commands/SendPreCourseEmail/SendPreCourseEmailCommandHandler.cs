using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Templates.Commands.SendPreCourseEmail;

public class SendPreCourseEmailCommandHandler
    : IRequestHandler<SendPreCourseEmailCommand, SendPreCourseEmailResult>
{
    private const string CcAddress    = "info@bagile.co.uk";
    private const string DefaultTimes = "09:00 – 17:00";

    private readonly ICourseScheduleQueries    _scheduleQueries;
    private readonly IPreCourseTemplateRepository _templateRepo;
    private readonly IEmailSendLogRepository   _logRepo;
    private readonly IEmailService             _emailService;
    private readonly ILogger<SendPreCourseEmailCommandHandler> _logger;

    public SendPreCourseEmailCommandHandler(
        ICourseScheduleQueries scheduleQueries,
        IPreCourseTemplateRepository templateRepo,
        IEmailSendLogRepository logRepo,
        IEmailService emailService,
        ILogger<SendPreCourseEmailCommandHandler> logger)
    {
        _scheduleQueries = scheduleQueries;
        _templateRepo    = templateRepo;
        _logRepo         = logRepo;
        _emailService    = emailService;
        _logger          = logger;
    }

    public async Task<SendPreCourseEmailResult> Handle(
        SendPreCourseEmailCommand request, CancellationToken ct)
    {
        // 1. Load course schedule
        var course = await _scheduleQueries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct)
            ?? throw new KeyNotFoundException(
                $"Course schedule {request.CourseScheduleId} not found.");

        // 2. Resolve course type and format
        var courseType = DeriveCourseType(course.CourseCode);
        var format     = ResolveFormat(request.FormatOverride, course.FormatType);

        // 3. Load template (required unless caller supplies a full HTML override)
        var template = await _templateRepo.GetAsync(courseType, format, ct);
        if (template is null && string.IsNullOrWhiteSpace(request.HtmlBodyOverride))
            throw new InvalidOperationException(
                $"No pre-course template found for course type '{courseType}' format '{format}'. " +
                $"Create one via PUT /api/templates/pre-course/{courseType}?format={format} " +
                $"or supply htmlBodyOverride in the request.");

        // 4. Load active attendees
        var attendees = (await _scheduleQueries.GetCourseAttendeesAsync(request.CourseScheduleId, ct))
            .Where(a => a.Status == "active")
            .ToList();

        if (attendees.Count == 0)
            throw new InvalidOperationException(
                $"Course {request.CourseScheduleId} has no active attendees — email not sent.");

        var toEmails = attendees.Select(a => a.Email).Distinct().ToList();

        // 5. Build variables
        var variables = BuildVariables(course, courseType);

        // 6. Apply variables to subject and body
        var subjectTemplate = template?.SubjectTemplate ?? "";
        var subject  = TemplateVariableSubstitution.Apply(subjectTemplate, variables);

        var bodyTemplate = request.HtmlBodyOverride ?? template!.HtmlBody;
        var htmlBody = TemplateVariableSubstitution.Apply(bodyTemplate, variables);

        // 7. Send
        _logger.LogInformation(
            "Sending pre-course email for course {CourseId} ({CourseCode}) format={Format} to {Count} recipients",
            request.CourseScheduleId, course.CourseCode, format, toEmails.Count);

        await _emailService.SendAsync(
            to:       toEmails,
            subject:  subject,
            htmlBody: htmlBody,
            cc:       [CcAddress],
            ct:       ct);

        // 8. Audit log
        await _logRepo.LogAsync(new EmailSendLog
        {
            CourseScheduleId = (int)request.CourseScheduleId,
            TemplateType     = "pre_course",
            RecipientCount   = toEmails.Count,
            Recipients       = string.Join(", ", toEmails),
            Subject          = subject,
            IsTest           = false
        }, ct);

        return new SendPreCourseEmailResult
        {
            RecipientCount  = toEmails.Count,
            Subject         = subject,
            CourseType      = courseType,
            Format          = format,
            RecipientEmails = toEmails.AsReadOnly()
        };
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    internal static string DeriveCourseType(string courseCode)
    {
        var prefix = courseCode.Split('-')[0];
        return prefix.ToUpper();
    }

    internal static string ResolveFormat(string? formatOverride, string? courseFormatType)
    {
        if (!string.IsNullOrWhiteSpace(formatOverride))
            return formatOverride.ToLower();

        // course_schedules.format_type stores 'virtual' or 'in_person'
        return courseFormatType?.ToLower() switch
        {
            "in_person" => "f2f",
            "f2f"       => "f2f",
            _           => "virtual"
        };
    }

    internal static IReadOnlyDictionary<string, string> BuildVariables(
        Application.CourseSchedules.DTOs.CourseScheduleDetailDto course,
        string courseType)
    {
        var dates  = BuildCourseDates(course.StartDate, course.EndDate);
        var agenda = BuildDefaultAgenda(courseType);
        var selfStudy = BuildDefaultSelfStudy(courseType);

        // client_name: for private courses the title contains the client org; for public use course type
        var clientName = string.IsNullOrWhiteSpace(course.Location) ? courseType : course.Location;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["course_name"]    = course.Title,
            ["dates"]          = dates,
            ["times"]          = DefaultTimes,
            ["trainer_name"]   = course.TrainerName ?? "Alex and Chris",
            ["venue_address"]  = course.VenueAddress ?? "",
            ["zoom_url"]       = course.MeetingUrl ?? "",
            ["zoom_id"]        = course.MeetingId ?? "",
            ["zoom_passcode"]  = course.MeetingPasscode ?? "",
            ["client_name"]    = clientName,
            ["self_study"]     = selfStudy,
            ["agenda"]         = agenda,
        };
    }

    private static string BuildCourseDates(DateTime? start, DateTime? end)
    {
        if (start is null) return "";
        var startStr = start.Value.ToString("d MMMM yyyy");
        if (end is null || end.Value.Date == start.Value.Date) return startStr;
        return $"{startStr} – {end.Value:d MMMM yyyy}";
    }

    private static string BuildDefaultSelfStudy(string courseType) => courseType switch
    {
        "PSM" or "PSMAI" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li>Read the <a href=""https://www.scrum.org/resources/evidence-based-management-guide"">EBM Guide</a></li>
  <li><a href=""https://www.scrum.org/open-assessments/scrum-open"">Scrum Open assessment</a> — aim for 85%+ before you arrive</li>
</ul>",
        "PSPO" or "PSPOAI" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li><a href=""https://www.scrum.org/open-assessments/product-owner-open"">Product Owner Open assessment</a> — aim for 85%+ before you arrive</li>
</ul>",
        _ => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
</ul>"
    };

    private static string BuildDefaultAgenda(string courseType) => courseType switch
    {
        "PSM" or "PSMAI" => @"<p><strong>Day 1 AM:</strong> Introductions &amp; class agreements, Scrum Theory &amp; Empiricism<br>
<strong>Day 1 PM:</strong> The Scrum Framework, Done &amp; Undone<br>
<strong>Day 2 AM:</strong> Done &amp; Undone, Product Delivery with Scrum<br>
<strong>Day 2 PM:</strong> People &amp; Teams, The Scrum Master, Closing</p>",
        "PSPO" or "PSPOAI" => @"<p><strong>Day 1 AM:</strong> Introductions &amp; class agreements, Scrum Theory, The Product Owner role<br>
<strong>Day 1 PM:</strong> Product Vision &amp; Goals, Product Backlog<br>
<strong>Day 2 AM:</strong> Stakeholder engagement, Release planning<br>
<strong>Day 2 PM:</strong> Product metrics, Closing &amp; next steps</p>",
        _ => @"<p>Your trainer will share the detailed agenda on the day.</p>"
    };
}
