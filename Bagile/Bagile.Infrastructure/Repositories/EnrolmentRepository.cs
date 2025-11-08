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

        public async Task<int> CountByOrderIdAsync(long orderId)
        {
            const string sql = "SELECT COUNT(*) FROM bagile.enrolments WHERE order_id = @orderId;";
            await using var conn = new NpgsqlConnection(_conn);
            return await conn.ExecuteScalarAsync<int>(sql, new {orderId});
        }

        public async Task<long> InsertAsync(Enrolment enrolment)
        {
            const string sql = @"
        INSERT INTO bagile.enrolments
            (student_id, order_id, course_schedule_id, status,
             transferred_from_enrolment_id, original_sku,
             transfer_reason, transfer_notes, refund_eligible)
        VALUES
            (@StudentId, @OrderId, @CourseScheduleId, @Status,
             @TransferredFromEnrolmentId, @OriginalSku,
             @TransferReason, @TransferNotes, @RefundEligible)
        RETURNING id;";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.ExecuteScalarAsync<long>(sql, enrolment);
        }

        public async Task<Enrolment?> FindByOrderStudentAndSkuAsync(
            long orderId,
            long studentId,
            string sku)
        {
            const string sql = @"
        SELECT e.*
        FROM bagile.enrolments e
        JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
        WHERE e.order_id = @orderId
          AND e.student_id = @studentId
          AND cs.sku = @sku
          AND e.status != 'transferred';";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.QueryFirstOrDefaultAsync<Enrolment>(
                sql,
                new {orderId, studentId, sku});
        }

        public async Task UpdateStatusAsync(
            long enrolmentId,
            string status,
            long? transferredToEnrolmentId = null)
        {
            const string sql = @"
        UPDATE bagile.enrolments
        SET status = @status,
            transferred_to_enrolment_id = @transferredToEnrolmentId,
            updated_at = NOW()
        WHERE id = @enrolmentId;";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.ExecuteAsync(sql, new {enrolmentId, status, transferredToEnrolmentId});
        }

        public async Task<IEnumerable<Enrolment>> GetByOrderIdAsync(long orderId)
        {
            const string sql = @"
        SELECT * FROM bagile.enrolments
        WHERE order_id = @orderId
        ORDER BY created_at;";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.QueryAsync<Enrolment>(sql, new {orderId});
        }
    }
}