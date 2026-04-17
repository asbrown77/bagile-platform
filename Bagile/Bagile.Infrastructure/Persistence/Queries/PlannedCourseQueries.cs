using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.DTOs;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class PlannedCourseQueries : IPlannedCourseQueries
{
    private readonly string _connectionString;

    public PlannedCourseQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<PlannedCourseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT pc.id AS Id,
                   pc.course_type AS CourseType,
                   pc.trainer_id AS TrainerId,
                   t.name AS TrainerName,
                   pc.start_date AS StartDate,
                   pc.end_date AS EndDate,
                   pc.is_virtual AS IsVirtual,
                   pc.venue AS Venue,
                   pc.notes AS Notes,
                   pc.decision_deadline AS DecisionDeadline,
                   pc.is_private AS IsPrivate,
                   pc.status AS Status,
                   pc.created_at AS CreatedAt,
                   pc.updated_at AS UpdatedAt
            FROM bagile.planned_courses pc
            JOIN bagile.trainers t ON t.id = pc.trainer_id
            WHERE pc.id = @id;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<PlannedCourseDto>(sql, new { id });
    }

    public async Task<IEnumerable<PlannedCourseDto>> GetAllAsync(CancellationToken ct = default)
    {
        // LEFT JOIN so an orphaned trainer_id doesn't drop the row from the list.
        const string sql = @"
            SELECT pc.id AS Id,
                   pc.course_type AS CourseType,
                   pc.trainer_id AS TrainerId,
                   t.name AS TrainerName,
                   pc.start_date AS StartDate,
                   pc.end_date AS EndDate,
                   pc.is_virtual AS IsVirtual,
                   pc.venue AS Venue,
                   pc.notes AS Notes,
                   pc.decision_deadline AS DecisionDeadline,
                   pc.is_private AS IsPrivate,
                   pc.status AS Status,
                   pc.created_at AS CreatedAt,
                   pc.updated_at AS UpdatedAt
            FROM bagile.planned_courses pc
            LEFT JOIN bagile.trainers t ON t.id = pc.trainer_id
            ORDER BY pc.start_date;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<PlannedCourseDto>(new CommandDefinition(sql, cancellationToken: ct));
    }
}
