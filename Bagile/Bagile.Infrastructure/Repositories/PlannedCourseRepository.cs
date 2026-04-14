using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class PlannedCourseRepository : IPlannedCourseRepository
{
    private readonly string _connStr;

    public PlannedCourseRepository(string connStr)
    {
        _connStr = connStr;
    }

    public async Task<int> InsertAsync(PlannedCourse course)
    {
        const string sql = @"
            INSERT INTO bagile.planned_courses
                (course_type, trainer_id, start_date, end_date,
                 is_virtual, venue, notes, decision_deadline, is_private, status)
            VALUES
                (@CourseType, @TrainerId, @StartDate, @EndDate,
                 @IsVirtual, @Venue, @Notes, @DecisionDeadline, @IsPrivate, @Status)
            RETURNING id;";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.ExecuteScalarAsync<int>(sql, course);
    }

    public async Task<PlannedCourse?> GetByIdAsync(int id)
    {
        const string sql = @"
            SELECT id, course_type AS CourseType, trainer_id AS TrainerId,
                   start_date AS StartDate, end_date AS EndDate,
                   is_virtual AS IsVirtual, venue, notes,
                   decision_deadline AS DecisionDeadline,
                   is_private AS IsPrivate, status,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM bagile.planned_courses
            WHERE id = @id;";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryFirstOrDefaultAsync<PlannedCourse>(sql, new { id });
    }

    public async Task UpdateAsync(int id, PlannedCourse course)
    {
        const string sql = @"
            UPDATE bagile.planned_courses
            SET course_type       = @CourseType,
                trainer_id        = @TrainerId,
                start_date        = @StartDate,
                end_date          = @EndDate,
                is_virtual        = @IsVirtual,
                venue             = @Venue,
                notes             = @Notes,
                decision_deadline = @DecisionDeadline,
                is_private        = @IsPrivate
            WHERE id = @Id;";

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(sql, new
        {
            Id = id,
            course.CourseType,
            course.TrainerId,
            course.StartDate,
            course.EndDate,
            course.IsVirtual,
            course.Venue,
            course.Notes,
            course.DecisionDeadline,
            course.IsPrivate
        });
    }

    public async Task<bool> DeleteAsync(int id)
    {
        const string sql = "DELETE FROM bagile.planned_courses WHERE id = @id;";
        await using var conn = new NpgsqlConnection(_connStr);
        var rows = await conn.ExecuteAsync(sql, new { id });
        return rows > 0;
    }

    public async Task<bool> HasPublicationsAsync(int id)
    {
        const string sql = @"
            SELECT EXISTS(
                SELECT 1 FROM bagile.course_publications
                WHERE planned_course_id = @id
            );";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.ExecuteScalarAsync<bool>(sql, new { id });
    }
}
