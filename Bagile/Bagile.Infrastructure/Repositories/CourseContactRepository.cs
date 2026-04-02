using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class CourseContactRepository : ICourseContactRepository
{
    private readonly string _conn;

    public CourseContactRepository(string conn) => _conn = conn;

    public async Task<IEnumerable<CourseContact>> GetByCourseScheduleAsync(
        long courseScheduleId,
        CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id,
                   course_schedule_id AS CourseScheduleId,
                   role, name, email, phone,
                   created_at AS CreatedAt
            FROM bagile.course_contacts
            WHERE course_schedule_id = @courseScheduleId
            ORDER BY created_at;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QueryAsync<CourseContact>(
            new CommandDefinition(sql, new { courseScheduleId }, cancellationToken: ct));
    }

    public async Task<CourseContact> AddAsync(
        CourseContact contact,
        CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO bagile.course_contacts
                (course_schedule_id, role, name, email, phone)
            VALUES
                (@CourseScheduleId, @Role, @Name, @Email, @Phone)
            RETURNING id,
                      course_schedule_id AS CourseScheduleId,
                      role, name, email, phone,
                      created_at AS CreatedAt;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleAsync<CourseContact>(
            new CommandDefinition(sql, contact, cancellationToken: ct));
    }

    public async Task<bool> DeleteAsync(
        long courseScheduleId,
        long contactId,
        CancellationToken ct = default)
    {
        const string sql = @"
            DELETE FROM bagile.course_contacts
            WHERE id = @contactId
              AND course_schedule_id = @courseScheduleId;";

        await using var c = new NpgsqlConnection(_conn);
        var rows = await c.ExecuteAsync(
            new CommandDefinition(sql, new { contactId, courseScheduleId }, cancellationToken: ct));
        return rows > 0;
    }

    public async Task<CourseContact?> UpdateAsync(
        long courseScheduleId,
        long contactId,
        string role,
        string name,
        string email,
        string? phone,
        CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE bagile.course_contacts
               SET role  = @role,
                   name  = @name,
                   email = @email,
                   phone = @phone
             WHERE id = @contactId
               AND course_schedule_id = @courseScheduleId
            RETURNING id,
                      course_schedule_id AS CourseScheduleId,
                      role, name, email, phone,
                      created_at AS CreatedAt;";

        await using var c = new NpgsqlConnection(_conn);
        return await c.QuerySingleOrDefaultAsync<CourseContact>(
            new CommandDefinition(sql, new { contactId, courseScheduleId, role, name, email, phone },
                cancellationToken: ct));
    }
}
