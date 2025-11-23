using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories
{
    public class RawTransferRepository : IRawTransferRepository
    {
        private readonly string _conn;
        public RawTransferRepository(string conn) => _conn = conn;

        public async Task InsertAsync(RawTransfer transfer, CancellationToken token)
        {
            const string sql = @"
                INSERT INTO bagile.raw_transfers
                    (order_id, course_schedule_id, from_student_email, to_student_email, reason)
                VALUES
                    (@OrderId, @CourseScheduleId, @FromStudentEmail, @ToStudentEmail, @Reason);";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.ExecuteAsync(sql, transfer);
        }

        public async Task<IEnumerable<RawTransfer>> GetByOrderIdAsync(long orderId, CancellationToken token)
        {
            const string sql = @"
                SELECT id, order_id AS OrderId, course_schedule_id AS CourseScheduleId,
                       from_student_email AS FromStudentEmail, to_student_email AS ToStudentEmail,
                       reason, created_at AS CreatedAt
                FROM bagile.raw_transfers
                WHERE order_id = @orderId
                ORDER BY created_at;";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.QueryAsync<RawTransfer>(sql, new { orderId });
        }
    }
}