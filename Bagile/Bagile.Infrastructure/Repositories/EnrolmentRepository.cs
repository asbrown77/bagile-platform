using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class EnrolmentRepository : IEnrolmentRepository
{
    private readonly string _conn;
    public EnrolmentRepository(string conn) => _conn = conn;

    public async Task UpsertAsync(long studentId, long orderId, long? courseScheduleId)
    {
        const string sql = @"
                INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
                VALUES (@studentId, @orderId, @courseScheduleId)
                ON CONFLICT (student_id, order_id, course_schedule_id) DO NOTHING;";

        await using var c = new NpgsqlConnection(_conn);
        await c.ExecuteAsync(sql, new { studentId, orderId, courseScheduleId });
    }
}