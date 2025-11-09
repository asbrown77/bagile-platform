using Bagile.Application.Common.Interfaces;
using Bagile.Application.Transfers.DTOs;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class TransferQueries : ITransferQueries
{
    private readonly string _connectionString;

    public TransferQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<TransferDto>> GetTransfersAsync(
        DateTime? from,
        DateTime? to,
        string? reason,
        string? organisationName,
        long? courseScheduleId,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = @"
            WITH transfer_pairs AS (
                -- Find students with multiple enrolments for the same course code (SKU)
                SELECT 
                    e1.id AS from_enrolment_id,
                    e1.student_id,
                    e1.course_schedule_id AS from_schedule_id,
                    e1.created_at AS from_date,
                    e2.id AS to_enrolment_id,
                    e2.course_schedule_id AS to_schedule_id,
                    e2.created_at AS to_date,
                    cs1.sku AS course_code,
                    cs1.status AS from_status,
                    cs2.status AS to_status
                FROM bagile.enrolments e1
                JOIN bagile.course_schedules cs1 ON e1.course_schedule_id = cs1.id
                JOIN bagile.enrolments e2 ON e2.student_id = e1.student_id 
                    AND e2.id != e1.id
                JOIN bagile.course_schedules cs2 ON e2.course_schedule_id = cs2.id
                WHERE cs1.sku = cs2.sku
                    AND e2.created_at > e1.created_at
            )
            SELECT 
                s.id AS StudentId,
                CONCAT(s.first_name, ' ', s.last_name) AS StudentName,
                s.email AS StudentEmail,
                s.company AS Organisation,
                
                tp.from_schedule_id AS FromScheduleId,
                cs_from.sku AS FromCourseCode,
                cs_from.name AS FromCourseTitle,
                cs_from.start_date AS FromCourseStartDate,
                cs_from.status AS FromCourseStatus,
                
                tp.to_schedule_id AS ToScheduleId,
                cs_to.sku AS ToCourseCode,
                cs_to.name AS ToCourseTitle,
                cs_to.start_date AS ToCourseStartDate,
                cs_to.status AS ToCourseStatus,
                
                CASE 
                    WHEN cs_from.status = 'cancelled' THEN 'CourseCancelled'
                    ELSE 'StudentRequest'
                END AS Reason,
                tp.to_date AS TransferDate
            FROM transfer_pairs tp
            JOIN bagile.students s ON tp.student_id = s.id
            JOIN bagile.course_schedules cs_from ON tp.from_schedule_id = cs_from.id
            JOIN bagile.course_schedules cs_to ON tp.to_schedule_id = cs_to.id
            WHERE 1=1
            " + (from.HasValue ? " AND tp.to_date >= @from" : "") + @"
            " + (to.HasValue ? " AND tp.to_date <= @to" : "") + @"
            " + (reason != null ? @" AND CASE 
                    WHEN cs_from.status = 'cancelled' THEN 'CourseCancelled'
                    ELSE 'StudentRequest'
                END = @reason" : "") + @"
            " + (organisationName != null ? " AND s.company ILIKE @organisationPattern" : "") + @"
            " + (courseScheduleId.HasValue ? " AND (tp.from_schedule_id = @courseScheduleId OR tp.to_schedule_id = @courseScheduleId)" : "") + @"
            ORDER BY tp.to_date DESC
            LIMIT @pageSize OFFSET @offset;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<TransferDto>(sql, new
        {
            from,
            to,
            reason,
            organisationPattern = organisationName != null ? $"%{organisationName}%" : null,
            courseScheduleId,
            pageSize,
            offset = (page - 1) * pageSize
        });
    }

    public async Task<int> CountTransfersAsync(
        DateTime? from,
        DateTime? to,
        string? reason,
        string? organisationName,
        long? courseScheduleId,
        CancellationToken ct = default)
    {
        var sql = @"
            WITH transfer_pairs AS (
                SELECT 
                    e1.id AS from_enrolment_id,
                    e1.student_id,
                    e1.course_schedule_id AS from_schedule_id,
                    e1.created_at AS from_date,
                    e2.id AS to_enrolment_id,
                    e2.course_schedule_id AS to_schedule_id,
                    e2.created_at AS to_date,
                    cs1.sku AS course_code,
                    cs1.status AS from_status,
                    cs2.status AS to_status
                FROM bagile.enrolments e1
                JOIN bagile.course_schedules cs1 ON e1.course_schedule_id = cs1.id
                JOIN bagile.enrolments e2 ON e2.student_id = e1.student_id 
                    AND e2.id != e1.id
                JOIN bagile.course_schedules cs2 ON e2.course_schedule_id = cs2.id
                WHERE cs1.sku = cs2.sku
                    AND e2.created_at > e1.created_at
            )
            SELECT COUNT(*)
            FROM transfer_pairs tp
            JOIN bagile.students s ON tp.student_id = s.id
            JOIN bagile.course_schedules cs_from ON tp.from_schedule_id = cs_from.id
            WHERE 1=1
            " + (from.HasValue ? " AND tp.to_date >= @from" : "") + @"
            " + (to.HasValue ? " AND tp.to_date <= @to" : "") + @"
            " + (reason != null ? @" AND CASE 
                    WHEN cs_from.status = 'cancelled' THEN 'CourseCancelled'
                    ELSE 'StudentRequest'
                END = @reason" : "") + @"
            " + (organisationName != null ? " AND s.company ILIKE @organisationPattern" : "") + @"
            " + (courseScheduleId.HasValue ? " AND (tp.from_schedule_id = @courseScheduleId OR tp.to_schedule_id = @courseScheduleId)" : "");

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            from,
            to,
            reason,
            organisationPattern = organisationName != null ? $"%{organisationName}%" : null,
            courseScheduleId
        });
    }

    public async Task<IEnumerable<PendingTransferDto>> GetPendingTransfersAsync(
        CancellationToken ct = default)
    {
        var sql = @"
            WITH cancelled_enrolments AS (
                -- Students enrolled in cancelled courses
                SELECT DISTINCT
                    e.student_id,
                    e.course_schedule_id,
                    cs.sku AS course_code,
                    cs.name AS course_title,
                    cs.start_date AS original_start_date,
                    cs.updated_at AS cancelled_date
                FROM bagile.enrolments e
                JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
                WHERE cs.status = 'cancelled'
            ),
            future_enrolments AS (
                -- Check if student has any future enrolments for the same course
                SELECT DISTINCT
                    e.student_id,
                    cs.sku AS course_code
                FROM bagile.enrolments e
                JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
                WHERE cs.status != 'cancelled'
                    AND cs.start_date > CURRENT_DATE
            )
            SELECT 
                s.id AS StudentId,
                CONCAT(s.first_name, ' ', s.last_name) AS StudentName,
                s.email AS StudentEmail,
                s.company AS Organisation,
                ce.course_schedule_id AS CancelledScheduleId,
                ce.course_code AS CourseCode,
                ce.course_title AS CourseTitle,
                ce.original_start_date AS OriginalStartDate,
                ce.cancelled_date AS CancelledDate,
                EXTRACT(DAY FROM (CURRENT_TIMESTAMP - ce.cancelled_date))::INT AS DaysSinceCancellation
            FROM cancelled_enrolments ce
            JOIN bagile.students s ON ce.student_id = s.id
            LEFT JOIN future_enrolments fe ON ce.student_id = fe.student_id 
                AND ce.course_code = fe.course_code
            WHERE fe.student_id IS NULL  -- No future enrolment for same course
            ORDER BY ce.cancelled_date DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<PendingTransferDto>(sql);
    }

    public async Task<TransfersByCourseDto> GetTransfersByCourseAsync(
        long scheduleId,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        // Get course info
        var courseInfoSql = @"
            SELECT 
                id AS CourseScheduleId,
                sku AS CourseCode,
                name AS CourseTitle,
                start_date AS StartDate,
                status AS Status
            FROM bagile.course_schedules
            WHERE id = @scheduleId;";

        var courseInfo = await conn.QueryFirstOrDefaultAsync<TransfersByCourseDto>(
            courseInfoSql,
            new { scheduleId });

        if (courseInfo == null)
        {
            return new TransfersByCourseDto { CourseScheduleId = scheduleId };
        }

        // Get transfers OUT (students who left this course)
        var transfersOutSql = @"
            WITH course_sku AS (
                SELECT sku FROM bagile.course_schedules WHERE id = @scheduleId
            )
            SELECT 
                s.id AS StudentId,
                CONCAT(s.first_name, ' ', s.last_name) AS StudentName,
                s.email AS StudentEmail,
                e2.course_schedule_id AS ToScheduleId,
                cs2.name AS ToCourseTitle,
                cs2.start_date AS ToCourseStartDate,
                e2.created_at AS TransferDate
            FROM bagile.enrolments e1
            JOIN bagile.students s ON e1.student_id = s.id
            JOIN bagile.course_schedules cs1 ON e1.course_schedule_id = cs1.id
            JOIN bagile.enrolments e2 ON e2.student_id = e1.student_id 
                AND e2.id != e1.id
            JOIN bagile.course_schedules cs2 ON e2.course_schedule_id = cs2.id
            WHERE e1.course_schedule_id = @scheduleId
                AND cs1.sku = cs2.sku
                AND e2.created_at > e1.created_at
            ORDER BY e2.created_at DESC;";

        var transfersOut = await conn.QueryAsync<TransferOutDto>(
            transfersOutSql,
            new { scheduleId });

        // Get transfers IN (students who joined this course)
        var transfersInSql = @"
            WITH course_sku AS (
                SELECT sku FROM bagile.course_schedules WHERE id = @scheduleId
            )
            SELECT 
                s.id AS StudentId,
                CONCAT(s.first_name, ' ', s.last_name) AS StudentName,
                s.email AS StudentEmail,
                e1.course_schedule_id AS FromScheduleId,
                cs1.name AS FromCourseTitle,
                cs1.start_date AS FromCourseStartDate,
                e2.created_at AS TransferDate
            FROM bagile.enrolments e2
            JOIN bagile.students s ON e2.student_id = s.id
            JOIN bagile.course_schedules cs2 ON e2.course_schedule_id = cs2.id
            JOIN bagile.enrolments e1 ON e1.student_id = e2.student_id 
                AND e1.id != e2.id
            JOIN bagile.course_schedules cs1 ON e1.course_schedule_id = cs1.id
            WHERE e2.course_schedule_id = @scheduleId
                AND cs1.sku = cs2.sku
                AND e2.created_at > e1.created_at
            ORDER BY e2.created_at DESC;";

        var transfersIn = await conn.QueryAsync<TransferInDto>(
            transfersInSql,
            new { scheduleId });

        return courseInfo with
        {
            TransfersOut = transfersOut,
            TransfersIn = transfersIn,
            TotalTransfersOut = transfersOut.Count(),
            TotalTransfersIn = transfersIn.Count()
        };
    }
}