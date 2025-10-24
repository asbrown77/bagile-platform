using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories
{
    public class EnrolmentRepository : IEnrolmentRepository
    {
        private readonly string _conn;
        public EnrolmentRepository(string conn) => _conn = conn;

        public async Task UpsertAsync(Enrolment enrolment)
        {
            const string sql = @"
                INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
                VALUES (@StudentId, @OrderId, @CourseScheduleId)
                ON CONFLICT (student_id, order_id, course_schedule_id) DO NOTHING;";

            await using var c = new NpgsqlConnection(_conn);
            await c.ExecuteAsync(sql, enrolment);
        }
    }
}