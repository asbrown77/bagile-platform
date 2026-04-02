using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class PreCourseTemplateRepository : IPreCourseTemplateRepository
{
    private readonly string _conn;

    public PreCourseTemplateRepository(string conn) => _conn = conn;

    public async Task<IEnumerable<PreCourseTemplate>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, course_type AS CourseType, format, subject_template AS SubjectTemplate,
                   html_body AS HtmlBody, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM bagile.pre_course_templates
            ORDER BY course_type, format;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QueryAsync<PreCourseTemplate>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<PreCourseTemplate?> GetAsync(
        string courseType,
        string format,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, course_type AS CourseType, format, subject_template AS SubjectTemplate,
                   html_body AS HtmlBody, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM bagile.pre_course_templates
            WHERE course_type = @courseType
              AND format      = @format;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleOrDefaultAsync<PreCourseTemplate>(
            new CommandDefinition(sql, new { courseType, format }, cancellationToken: ct));
    }

    public async Task<PreCourseTemplate> UpsertAsync(PreCourseTemplate template, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO bagile.pre_course_templates
                (course_type, format, subject_template, html_body, updated_at)
            VALUES
                (@CourseType, @Format, @SubjectTemplate, @HtmlBody, NOW())
            ON CONFLICT (course_type, format) DO UPDATE
            SET subject_template = EXCLUDED.subject_template,
                html_body        = EXCLUDED.html_body,
                updated_at       = NOW()
            RETURNING id, course_type AS CourseType, format,
                      subject_template AS SubjectTemplate,
                      html_body AS HtmlBody, created_at AS CreatedAt, updated_at AS UpdatedAt;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleAsync<PreCourseTemplate>(
            new CommandDefinition(sql, template, cancellationToken: ct));
    }
}
