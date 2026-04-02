using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IEmailSendLogRepository
{
    Task<EmailSendLog> LogAsync(EmailSendLog entry, CancellationToken ct = default);

    Task<IEnumerable<EmailSendLog>> GetByCourseScheduleAsync(
        int courseScheduleId,
        CancellationToken ct = default);
}
