using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class EmailSendLogRepository : IEmailSendLogRepository
{
    private readonly string _conn;

    public EmailSendLogRepository(string conn) => _conn = conn;

    public async Task<EmailSendLog> LogAsync(EmailSendLog entry, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO bagile.email_send_log
                (course_schedule_id, template_type, sent_by, recipient_count, recipients, subject, is_test)
            VALUES
                (@CourseScheduleId, @TemplateType, @SentBy, @RecipientCount, @Recipients, @Subject, @IsTest)
            RETURNING id, course_schedule_id AS CourseScheduleId, template_type AS TemplateType,
                      sent_by AS SentBy, recipient_count AS RecipientCount, recipients,
                      subject, is_test AS IsTest, sent_at AS SentAt;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleAsync<EmailSendLog>(
            new CommandDefinition(sql, entry, cancellationToken: ct));
    }

    public async Task<IEnumerable<EmailSendLog>> GetByCourseScheduleAsync(
        int courseScheduleId,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, course_schedule_id AS CourseScheduleId, template_type AS TemplateType,
                   sent_by AS SentBy, recipient_count AS RecipientCount, recipients,
                   subject, is_test AS IsTest, sent_at AS SentAt
            FROM bagile.email_send_log
            WHERE course_schedule_id = @courseScheduleId
            ORDER BY sent_at DESC;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QueryAsync<EmailSendLog>(
            new CommandDefinition(sql, new { courseScheduleId }, cancellationToken: ct));
    }
}
