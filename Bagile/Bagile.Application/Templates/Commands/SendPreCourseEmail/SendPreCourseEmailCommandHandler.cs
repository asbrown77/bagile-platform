using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.Application.Templates;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Templates.Commands.SendPreCourseEmail;

public class SendPreCourseEmailCommandHandler
    : IRequestHandler<SendPreCourseEmailCommand, SendPreCourseEmailResult>
{
    private const string CcAddress    = "info@bagile.co.uk";
    private const string DefaultTimes = "09:00 – 17:00";

    private readonly ICourseScheduleQueries      _scheduleQueries;
    private readonly IPreCourseTemplateRepository _templateRepo;
    private readonly IEmailSendLogRepository     _logRepo;
    private readonly ITrainerRepository          _trainerRepo;
    private readonly IEmailService               _emailService;
    private readonly ILogger<SendPreCourseEmailCommandHandler> _logger;

    public SendPreCourseEmailCommandHandler(
        ICourseScheduleQueries scheduleQueries,
        IPreCourseTemplateRepository templateRepo,
        IEmailSendLogRepository logRepo,
        ITrainerRepository trainerRepo,
        IEmailService emailService,
        ILogger<SendPreCourseEmailCommandHandler> logger)
    {
        _scheduleQueries = scheduleQueries;
        _templateRepo    = templateRepo;
        _logRepo         = logRepo;
        _trainerRepo     = trainerRepo;
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
        var htmlBody = EmailTemplateWrapper.Wrap(TemplateVariableSubstitution.Apply(bodyTemplate, variables));

        // 7. Resolve trainer email for Reply-To (best-effort — fallback to null)
        var replyTo = await ResolveTrainerEmailAsync(course.TrainerName, ct);

        // 8. Send
        _logger.LogInformation(
            "Sending pre-course email for course {CourseId} ({CourseCode}) format={Format} to {Count} recipients",
            request.CourseScheduleId, course.CourseCode, format, toEmails.Count);

        await _emailService.SendAsync(
            to:       toEmails,
            subject:  subject,
            htmlBody: htmlBody,
            cc:       [CcAddress],
            replyTo:  replyTo,
            ct:       ct);

        // 9. Audit log
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

    /// <summary>
    /// Resolve the trainer's email from the trainers table by matching on trainer name.
    /// Returns null if not found — callers treat null as "no Reply-To header".
    /// </summary>
    private async Task<string?> ResolveTrainerEmailAsync(string? trainerName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(trainerName)) return null;

        var trainers = await _trainerRepo.GetAllActiveAsync(ct);
        var match = trainers.FirstOrDefault(t =>
            string.Equals(t.Name.Trim(), trainerName.Trim(), StringComparison.OrdinalIgnoreCase));

        return match?.Email;
    }

    // Multi-segment course type prefixes that must be preserved intact.
    // Order matters: longer/more-specific prefixes must come before shorter ones
    // so that e.g. "APS-SD" is matched before "APS".
    private static readonly string[] MultiSegmentPrefixes =
    [
        "APS-SD", "PAL-EBM", "PAL-E", "PSM-A", "PSPO-A",
    ];

    internal static string DeriveCourseType(string courseCode)
    {
        var upper = courseCode.ToUpper();
        foreach (var prefix in MultiSegmentPrefixes)
        {
            // Match when the code starts with the prefix followed by '-' or end-of-string
            if (upper.StartsWith(prefix + "-") || upper == prefix)
                return prefix;
        }

        // Simple single-segment case: PSM-260427-CB → PSM
        return upper.Split('-')[0];
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
            ["course_name"]      = course.Title,
            ["course_full_name"] = CourseFullName(courseType),
            ["dates"]            = dates,
            ["times"]            = DefaultTimes,
            ["trainer_name"]     = course.TrainerName ?? "Alex and Chris",
            ["venue_address"]    = course.VenueAddress ?? "",
            ["zoom_url"]         = course.MeetingUrl ?? "",
            ["zoom_id"]          = course.MeetingId ?? "",
            ["zoom_passcode"]    = course.MeetingPasscode ?? "",
            ["client_name"]      = clientName,
            ["self_study"]       = selfStudy,
            ["agenda"]           = agenda,
        };
    }

    private static string CourseFullName(string courseType) => courseType switch
    {
        "PSM" or "PSMAI"   => "Professional Scrum Master",
        "PSPO" or "PSPOAI" => "Professional Scrum Product Owner",
        "PSK"              => "Professional Scrum with Kanban",
        "APS-SD"           => "Applying Professional Scrum for Software Development",
        "PAL-E"            => "Professional Agile Leadership Essentials",
        "PAL-EBM"          => "Professional Agile Leadership with EBM",
        "PSPO-A"           => "Professional Scrum Product Owner Advanced",
        "PSM-A"            => "Professional Scrum Master Advanced",
        "PSFS"             => "Professional Scrum Facilitation Skills",
        "PSU"              => "Professional Scrum with User Experience",
        "EBM"              => "Evidence-Based Management",
        _                  => courseType
    };

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
  <li>Review the suggested reading for Product Owners on the <a href=""https://www.scrum.org/pathway/product-owner/"">Product Owner learning path</a></li>
  <li><a href=""https://www.scrum.org/open-assessments/product-owner-open"">Product Owner Open assessment</a> — aim for 85%+ before you arrive</li>
</ul>",
        "PSK" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li>Read the <a href=""https://www.scrum.org/resources/kanban-guide-scrum-teams"">Kanban Guide for Scrum Teams</a></li>
  <li><a href=""https://www.scrum.org/open-assessments/scrum-open"">Scrum Open assessment</a> — good warm-up</li>
</ul>",
        "APS-SD" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li>Basic programming knowledge is helpful — no specific language required</li>
  <li><a href=""https://www.scrum.org/open-assessments/scrum-open"">Scrum Open assessment</a> — good warm-up</li>
</ul>",
        "PAL-E" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li>Read the <a href=""https://www.scrum.org/resources/evidence-based-management-guide"">EBM Guide</a></li>
  <li>Reflect on your current leadership context — you will be asked to apply concepts to real situations</li>
</ul>",
        "PSPO-A" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li>Read the <a href=""https://www.scrum.org/resources/evidence-based-management-guide"">EBM Guide</a></li>
  <li>Review the <a href=""https://www.scrum.org/pathway/product-owner/"">Product Owner learning path</a> on scrum.org</li>
  <li><a href=""https://www.scrum.org/open-assessments/product-owner-open"">Product Owner Open assessment</a> — aim for 85%+ before you arrive</li>
</ul>",
        "PSM-A" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> thoroughly — many advanced discussions start here</li>
  <li>Review the <a href=""https://www.scrum.org/pathway/scrum-master/"">Scrum Master learning path</a> on scrum.org</li>
  <li>Reflect on situations where you have coached teams or navigated organisational challenges</li>
</ul>",
        "PSFS" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li>Browse the <a href=""https://www.scrum.org/courses/professional-scrum-facilitation-skills"">Facilitation Learning Series</a> on scrum.org</li>
  <li>Think about a Scrum event you have facilitated recently — what worked, what did not</li>
</ul>",
        "PSU" => @"<ul>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li>Basic UX understanding is helpful — no design experience required</li>
  <li>Review the <a href=""https://www.scrum.org/pathway/product-owner/"">Product Owner learning path</a> — PSU overlaps heavily with PO responsibilities</li>
</ul>",
        "EBM" => @"<ul>
  <li>Read the <a href=""https://www.scrum.org/resources/evidence-based-management-guide"">EBM Guide</a> (primary reference for this course)</li>
  <li>Read the <a href=""https://scrumguides.org"">Scrum Guide</a> (free at scrumguides.org)</li>
  <li>Think about how your organisation currently measures value and progress</li>
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
        "PSPO" or "PSPOAI" => @"<p><strong>Day 1 AM:</strong> Introductions, Product Value, Product Backlog Management<br>
<strong>Day 1 PM:</strong> Release Management, Product Backlog Refinement<br>
<strong>Day 2 AM:</strong> Stakeholders &amp; Customers, Forecasting &amp; Reporting<br>
<strong>Day 2 PM:</strong> Scaling, Product Owner in Practice, Closing</p>",
        "PSK" => @"<p><strong>Day 1 AM:</strong> Introductions, Scrum Theory &amp; Flow, Kanban Practices<br>
<strong>Day 1 PM:</strong> Flow Metrics, WIP Limits, Visualisation<br>
<strong>Day 2 AM:</strong> Service Level Expectations, Continuous Improvement<br>
<strong>Day 2 PM:</strong> Kanban with Scrum Events, Scaling Flow, Closing</p>",
        "APS-SD" => @"<p><strong>Day 1 AM:</strong> Introductions, Agile &amp; Scrum Foundations, Development Team<br>
<strong>Day 1 PM:</strong> Definition of Done, Technical Debt, Clean Code<br>
<strong>Day 2 AM:</strong> Test-Driven Development, Pair/Mob Programming<br>
<strong>Day 2 PM:</strong> Refactoring, CI/CD, Design Patterns<br>
<strong>Day 3 AM:</strong> Sprint simulation, Emerging Architecture<br>
<strong>Day 3 PM:</strong> Sprint simulation continued, Retrospective, Closing</p>",
        "PAL-E" => @"<p><strong>Day 1 AM:</strong> Introductions, Why Agility, Complexity &amp; Cynefin<br>
<strong>Day 1 PM:</strong> Agile Leadership, Organisational Design<br>
<strong>Day 2 AM:</strong> Evidence-Based Management, Measuring Value<br>
<strong>Day 2 PM:</strong> Leading Change, Coaching Leaders, Closing</p>",
        "PSPO-A" => @"<p><strong>Day 1 AM:</strong> Introductions, Product Vision &amp; Strategy, Stakeholder Management<br>
<strong>Day 1 PM:</strong> Product Backlog Management at Scale, Metrics &amp; Evidence, Closing</p>",
        "PSM-A" => @"<p><strong>Day 1 AM:</strong> Introductions, Scrum Theory Deep Dive, Facilitation<br>
<strong>Day 1 PM:</strong> Coaching Stances, Team Dynamics, Conflict<br>
<strong>Day 2 AM:</strong> Organisational Design, Servant Leadership<br>
<strong>Day 2 PM:</strong> Systems Thinking, Continuous Improvement, Closing</p>",
        "PSFS" => @"<p><strong>Day 1 AM:</strong> Introductions, Facilitation Principles, Facilitation Skills<br>
<strong>Day 1 PM:</strong> Facilitating Scrum Events, Practice Sessions, Closing</p>",
        "PSU" => @"<p><strong>Day 1 AM:</strong> Introductions, UX and Scrum, Lean UX<br>
<strong>Day 1 PM:</strong> User Research, Personas, Experiments<br>
<strong>Day 2 AM:</strong> Dual-Track Development, UX in Sprint<br>
<strong>Day 2 PM:</strong> Usability Testing, Stakeholder Alignment, Closing</p>",
        "EBM" => @"<p><strong>Day 1 AM:</strong> Introductions, Why EBM, The Four Key Value Areas<br>
<strong>Day 1 PM:</strong> Setting Goals, Forming Experiments, Measuring Outcomes, Closing</p>",
        _ => @"<p>Your trainer will share the detailed agenda on the day.</p>"
    };
}
