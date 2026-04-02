using Bagile.Application.Templates.DTOs;
using Bagile.Domain.Repositories;
using MediatR;

namespace Bagile.Application.Templates.Queries;

public class GetEmailSendLogQueryHandler
    : IRequestHandler<GetEmailSendLogQuery, IEnumerable<EmailSendLogDto>>
{
    private readonly IEmailSendLogRepository _repo;

    public GetEmailSendLogQueryHandler(IEmailSendLogRepository repo) => _repo = repo;

    public async Task<IEnumerable<EmailSendLogDto>> Handle(
        GetEmailSendLogQuery request, CancellationToken ct)
    {
        var entries = await _repo.GetByCourseScheduleAsync(request.CourseScheduleId, ct);
        return entries.Select(e => new EmailSendLogDto
        {
            Id               = e.Id,
            CourseScheduleId = e.CourseScheduleId,
            TemplateType     = e.TemplateType,
            SentBy           = e.SentBy,
            RecipientCount   = e.RecipientCount,
            Recipients       = e.Recipients,
            Subject          = e.Subject,
            IsTest           = e.IsTest,
            SentAt           = e.SentAt
        });
    }
}
