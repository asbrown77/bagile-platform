using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class PostCourseTemplateRepository : IPostCourseTemplateRepository
{
    private readonly string _conn;

    public PostCourseTemplateRepository(string conn) => _conn = conn;

    public async Task<IEnumerable<PostCourseTemplate>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, course_type AS CourseType, subject_template AS SubjectTemplate,
                   html_body AS HtmlBody, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM bagile.post_course_templates
            ORDER BY course_type;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QueryAsync<PostCourseTemplate>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task<PostCourseTemplate?> GetByCourseTypeAsync(string courseType, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, course_type AS CourseType, subject_template AS SubjectTemplate,
                   html_body AS HtmlBody, created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM bagile.post_course_templates
            WHERE course_type = @courseType;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleOrDefaultAsync<PostCourseTemplate>(
            new CommandDefinition(sql, new { courseType }, cancellationToken: ct));
    }

    public async Task<PostCourseTemplate> UpsertAsync(PostCourseTemplate template, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body, updated_at)
            VALUES (@CourseType, @SubjectTemplate, @HtmlBody, NOW())
            ON CONFLICT (course_type) DO UPDATE
            SET subject_template = EXCLUDED.subject_template,
                html_body        = EXCLUDED.html_body,
                updated_at       = NOW()
            RETURNING id, course_type AS CourseType, subject_template AS SubjectTemplate,
                      html_body AS HtmlBody, created_at AS CreatedAt, updated_at AS UpdatedAt;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleAsync<PostCourseTemplate>(
            new CommandDefinition(sql, template, cancellationToken: ct));
    }
}
