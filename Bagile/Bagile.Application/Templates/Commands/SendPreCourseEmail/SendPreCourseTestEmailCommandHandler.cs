using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.Application.Templates;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.Templates.Commands.SendPreCourseEmail;

public class SendPreCourseTestEmailCommandHandler
    : IRequestHandler<SendPreCourseTestEmailCommand, SendPreCourseTestEmailResult>
{
    private readonly ICourseScheduleQueries      _scheduleQueries;
    private readonly IPreCourseTemplateRepository _templateRepo;
    private readonly ITrainerRepository          _trainerRepo;
    private readonly IEmailSendLogRepository     _logRepo;
    private readonly IEmailService               _emailService;
    private readonly ILogger<SendPreCourseTestEmailCommandHandler> _logger;

    public SendPreCourseTestEmailCommandHandler(
        ICourseScheduleQueries scheduleQueries,
        IPreCourseTemplateRepository templateRepo,
        ITrainerRepository trainerRepo,
        IEmailSendLogRepository logRepo,
        IEmailService emailService,
        ILogger<SendPreCourseTestEmailCommandHandler> logger)
    {
        _scheduleQueries = scheduleQueries;
        _templateRepo    = templateRepo;
        _trainerRepo     = trainerRepo;
        _logRepo         = logRepo;
        _emailService    = emailService;
        _logger          = logger;
    }

    public async Task<SendPreCourseTestEmailResult> Handle(
        SendPreCourseTestEmailCommand request, CancellationToken ct)
    {
        // 1. Load course schedule
        var course = await _scheduleQueries.GetCourseScheduleByIdAsync(request.CourseScheduleId, ct)
            ?? throw new KeyNotFoundException(
                $"Course schedule {request.CourseScheduleId} not found.");

        // 2. Resolve course type and format
        var courseType = SendPreCourseEmailCommandHandler.DeriveCourseType(course.CourseCode);
        var format     = SendPreCourseEmailCommandHandler.ResolveFormat(request.FormatOverride, course.FormatType);

        // 3. Load template
        var template = await _templateRepo.GetAsync(courseType, format, ct);
        if (template is null && string.IsNullOrWhiteSpace(request.HtmlBodyOverride))
            throw new InvalidOperationException(
                $"No pre-course template found for course type '{courseType}' format '{format}'.");

        // 4. Resolve recipient — explicit > trainer lookup (DB) > fallback
        var recipientEmail = await ResolveRecipientAsync(request.RecipientEmail, course.TrainerName, ct);

        // 5. Build variables
        var variables = SendPreCourseEmailCommandHandler.BuildVariables(course, courseType);

        // 6. Apply
        var subjectTemplate = template?.SubjectTemplate ?? "";
        var subject  = $"[TEST] {TemplateVariableSubstitution.Apply(subjectTemplate, variables)}";
        var bodyTemplate = request.HtmlBodyOverride ?? template!.HtmlBody;
        var htmlBody = EmailTemplateWrapper.Wrap(TemplateVariableSubstitution.Apply(bodyTemplate, variables));

        // 7. Send to single recipient (no CC for test; reply-to = same recipient)
        _logger.LogInformation(
            "Sending pre-course test email for course {CourseId} ({CourseCode}) format={Format} to {Recipient}",
            request.CourseScheduleId, course.CourseCode, format, recipientEmail);

        await _emailService.SendAsync(
            to:       [recipientEmail],
            subject:  subject,
            htmlBody: htmlBody,
            cc:       [],
            replyTo:  recipientEmail,
            ct:       ct);

        // 8. Audit log
        await _logRepo.LogAsync(new EmailSendLog
        {
            CourseScheduleId = (int)request.CourseScheduleId,
            TemplateType     = "pre_course",
            RecipientCount   = 1,
            Recipients       = recipientEmail,
            Subject          = subject,
            IsTest           = true
        }, ct);

        return new SendPreCourseTestEmailResult
        {
            RecipientEmail = recipientEmail,
            Subject        = subject,
            CourseType     = courseType,
            Format         = format
        };
    }

    private async Task<string> ResolveRecipientAsync(
        string? explicitEmail,
        string? trainerName,
        CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(explicitEmail))
            return explicitEmail.Trim();

        if (!string.IsNullOrWhiteSpace(trainerName))
        {
            var trainers = await _trainerRepo.GetAllActiveAsync(ct);
            var match = trainers.FirstOrDefault(t =>
                string.Equals(t.Name.Trim(), trainerName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (match is not null)
                return match.Email;

            // Trainer name on course doesn't match any active trainer — fall through to default.
        }

        // Fallback: first active trainer, otherwise hardcoded safety net
        var allTrainers = await _trainerRepo.GetAllActiveAsync(ct);
        return allTrainers.FirstOrDefault()?.Email ?? "alexbrown@bagile.co.uk";
    }
}
