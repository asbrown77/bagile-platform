using Bagile.Application.Common.Interfaces;
using Bagile.Application.Enrolments.DTOs;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class EnrolmentQueries : IEnrolmentQueries
{
    private readonly string _connectionString;

    public EnrolmentQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<EnrolmentListDto>> GetEnrolmentsAsync(
        long? courseScheduleId,
        long? studentId,
        string? status,
        string? organisation,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = @"
            WITH enrolment_transfers AS (
                -- Detect transfers by finding students with multiple enrolments for same course code
                SELECT 
                    e1.id AS enrolment_id,
                    e2.course_schedule_id AS transfer_from_id,
                    e1.course_schedule_id AS transfer_to_id
                FROM bagile.enrolments e1
                JOIN bagile.course_schedules cs1 ON e1.course_schedule_id = cs1.id
                JOIN bagile.enrolments e2 ON e2.student_id = e1.student_id 
                    AND e2.id != e1.id
                JOIN bagile.course_schedules cs2 ON e2.course_schedule_id = cs2.id
                WHERE cs1.sku = cs2.sku
                    AND e1.created_at > e2.created_at
            )
            SELECT 
                e.id AS Id,
                COALESCE(e.course_schedule_id, 0) AS CourseScheduleId,
                COALESCE(cs.sku, 'PRIVATE') AS CourseCode,
                COALESCE(cs.name, 'Private Course') AS CourseTitle,
                cs.start_date AS CourseStartDate,
                s.id AS StudentId,
                CONCAT(s.first_name, ' ', s.last_name) AS StudentName,
                s.email AS StudentEmail,
                s.company AS Organisation,
                'Booked' AS Status,
                CASE WHEN et.enrolment_id IS NOT NULL THEN true ELSE false END AS IsTransfer,
                et.transfer_from_id AS TransferFromScheduleId,
                et.transfer_to_id AS TransferToScheduleId,
                e.created_at AS CreatedAt
            FROM bagile.enrolments e
            JOIN bagile.students s ON e.student_id = s.id
            LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            LEFT JOIN enrolment_transfers et ON e.id = et.enrolment_id
            WHERE 1=1
            " + (courseScheduleId.HasValue ? " AND e.course_schedule_id = @courseScheduleId" : "") + @"
            " + (studentId.HasValue ? " AND e.student_id = @studentId" : "") + @"
            " + (status != null ? " AND 'Booked' = @status" : "") + @"
            " + (organisation != null ? " AND s.company ILIKE @organisationPattern" : "") + @"
            " + (from.HasValue ? " AND cs.start_date >= @from" : "") + @"
            " + (to.HasValue ? " AND cs.start_date <= @to" : "") + @"
            ORDER BY cs.start_date DESC NULLS LAST, e.created_at DESC
            LIMIT @pageSize OFFSET @offset;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<EnrolmentListDto>(sql, new
        {
            courseScheduleId,
            studentId,
            status,
            organisationPattern = organisation != null ? $"%{organisation}%" : null,
            from,
            to,
            pageSize,
            offset = (page - 1) * pageSize
        });
    }

    public async Task<int> CountEnrolmentsAsync(
        long? courseScheduleId,
        long? studentId,
        string? status,
        string? organisation,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT COUNT(*)
            FROM bagile.enrolments e
            JOIN bagile.students s ON e.student_id = s.id
            LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            WHERE 1=1
            " + (courseScheduleId.HasValue ? " AND e.course_schedule_id = @courseScheduleId" : "") + @"
            " + (studentId.HasValue ? " AND e.student_id = @studentId" : "") + @"
            " + (status != null ? " AND 'Booked' = @status" : "") + @"
            " + (organisation != null ? " AND s.company ILIKE @organisationPattern" : "") + @"
            " + (from.HasValue ? " AND cs.start_date >= @from" : "") + @"
            " + (to.HasValue ? " AND cs.start_date <= @to" : "");

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            courseScheduleId,
            studentId,
            status,
            organisationPattern = organisation != null ? $"%{organisation}%" : null,
            from,
            to
        });
    }
}