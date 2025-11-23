using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            const string selectSql = @"
        SELECT id
        FROM bagile.enrolments
        WHERE student_id = @StudentId
          AND order_id = @OrderId
          AND course_schedule_id = @CourseScheduleId
          AND is_cancelled IS NOT TRUE
        LIMIT 1;";

            const string insertSql = @"
        INSERT INTO bagile.enrolments
            (student_id, order_id, course_schedule_id, status)
        VALUES
            (@StudentId, @OrderId, @CourseScheduleId, @Status);";

            const string updateSql = @"
        UPDATE bagile.enrolments
        SET status = @Status,
            updated_at = NOW()
        WHERE id = @Id;";

            await using var conn = new NpgsqlConnection(_conn);

            var existingId = await conn.ExecuteScalarAsync<long?>(selectSql, enrolment);

            if (existingId.HasValue)
            {
                // Update existing row unless transferred
                enrolment.Id = existingId.Value;

                await conn.ExecuteAsync(updateSql, new
                {
                    Id = existingId.Value,
                    enrolment.Status
                });
            }
            else
            {
                // Insert new normal enrolment
                await conn.ExecuteAsync(insertSql, enrolment);
            }
        }



        public async Task<int> CountByOrderIdAsync(long orderId)
        {
            const string sql = "SELECT COUNT(*) FROM bagile.enrolments WHERE order_id = @orderId;";
            await using var conn = new NpgsqlConnection(_conn);
            return await conn.ExecuteScalarAsync<int>(sql, new { orderId });
        }

        public async Task<Enrolment?> FindActiveByStudentEmailAsync(string email)
        {
            const string sql = @"
                SELECT e.*
                FROM bagile.enrolments e
                JOIN bagile.students s ON e.student_id = s.id
                WHERE s.email = @email
                  AND e.status = 'active'
                ORDER BY e.created_at DESC
                LIMIT 1;";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.QueryFirstOrDefaultAsync<Enrolment>(sql, new { email });
        }

        public async Task MarkTransferredAsync(long enrolmentId, long transferredToEnrolmentId)
        {
            const string sql = @"
                UPDATE bagile.enrolments
                SET status = 'transferred',
                    transferred_to_enrolment_id = @toId,
                    updated_at = NOW()
                WHERE id = @id;";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.ExecuteAsync(sql, new { id = enrolmentId, toId = transferredToEnrolmentId });
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
                new { orderId, studentId, sku });
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
            await conn.ExecuteAsync(sql, new { enrolmentId, status, transferredToEnrolmentId });
        }

        public async Task<IEnumerable<Enrolment>> GetByOrderIdAsync(long orderId)
        {
            const string sql = @"
                SELECT * 
                FROM bagile.enrolments
                WHERE order_id = @orderId
                ORDER BY created_at;";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.QueryAsync<Enrolment>(sql, new { orderId });
        }

        public async Task<Enrolment?> FindHeuristicTransferSourceAsync(
            long studentId,
            string courseFamilyPrefix)
        {
            const string sql = @"
                SELECT e.*
                FROM bagile.enrolments e
                JOIN bagile.course_schedules cs ON cs.id = e.course_schedule_id
                JOIN bagile.orders o ON o.id = e.order_id
                WHERE e.student_id = @studentId
                  AND e.status = 'active'
                  AND cs.sku ILIKE @skuPattern
                  --AND e.is_cancelled IS NOT TRUE         -- ignore cancelled
                ORDER BY e.created_at DESC
                LIMIT 1;";

            await using var conn = new NpgsqlConnection(_conn);
            var results = (await conn.QueryAsync<Enrolment>(
                sql,
                new { studentId, skuPattern = courseFamilyPrefix + "%" }
            )).ToList();

            if (results.Count == 1)
                return results[0];

            return null;
        }

        public async Task CancelEnrolmentAsync(long enrolmentId, string? reason = null)
        {
            const string sql = @"
        UPDATE bagile.enrolments
        SET is_cancelled = TRUE,
            status = 'cancelled',
            cancelled_at = NOW(),
            updated_at = NOW()
        WHERE id = @id;";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.ExecuteAsync(sql, new { id = enrolmentId });
        }

        public async Task<Enrolment?> FindAsync(long studentId, long orderId, long courseScheduleId)
        {
            const string sql = @"
        SELECT *
        FROM bagile.enrolments
        WHERE student_id = @studentId
          AND order_id = @orderId
          AND course_schedule_id = @courseScheduleId
        LIMIT 1;";

            await using var conn = new NpgsqlConnection(_conn);

            return await conn.QueryFirstOrDefaultAsync<Enrolment>(
                sql,
                new { studentId, orderId, courseScheduleId }
            );
        }


    }
}
