using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class CourseScheduleQueries : ICourseScheduleQueries
{
    private readonly string _connectionString;

    public CourseScheduleQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<CourseScheduleDto>> GetCourseSchedulesAsync(
        DateTime? from,
        DateTime? to,
        string? courseCode,
        string? trainer,
        string? type,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                cs.id AS Id,
                COALESCE(cs.sku, '') AS CourseCode,
                cs.name AS Title,
                cs.start_date AS StartDate,
                cs.end_date AS EndDate,
                cs.format_type AS Location,
                CASE WHEN cs.is_public THEN 'public' ELSE 'private' END AS Type,
                cs.status AS Status,
                COUNT(e.id) AS CurrentEnrolmentCount,
                CASE WHEN COUNT(e.id) >= 3 THEN true ELSE false END AS GuaranteedToRun,
                CASE 
                    WHEN cs.start_date <= CURRENT_DATE + INTERVAL '7 days' 
                         AND COUNT(e.id) < 3 
                    THEN true 
                    ELSE false 
                END AS NeedsAttention
            FROM bagile.course_schedules cs
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id
            WHERE 1=1
            " + (from != null ? " AND cs.start_date >= @from" : "") + @"
            " + (to != null ? " AND cs.start_date <= @to" : "") + @"
            " + (courseCode != null ? " AND cs.sku ILIKE @courseCodePattern" : "") + @"
            " + (trainer != null ? " AND cs.trainer_name ILIKE @trainerPattern" : "") + @"
            " + (type != null ? " AND ((cs.is_public = true AND @type = 'public') OR (cs.is_public = false AND @type = 'private'))" : "") + @"
            " + (status != null ? " AND cs.status = @status" : "") + @"
            GROUP BY cs.id, cs.sku, cs.name, cs.start_date, cs.end_date, cs.format_type, cs.is_public, cs.status
            ORDER BY cs.start_date DESC
            LIMIT @pageSize OFFSET @offset;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CourseScheduleDto>(sql, new
        {
            from,
            to,
            courseCodePattern = courseCode != null ? $"%{courseCode}%" : null,
            trainerPattern = trainer != null ? $"%{trainer}%" : null,
            type,
            status,
            pageSize,
            offset = (page - 1) * pageSize
        });
    }

    public async Task<int> CountCourseSchedulesAsync(
        DateTime? from,
        DateTime? to,
        string? courseCode,
        string? trainer,
        string? type,
        string? status,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT COUNT(DISTINCT cs.id)
            FROM bagile.course_schedules cs
            WHERE 1=1
            " + (from != null ? " AND cs.start_date >= @from" : "") + @"
            " + (to != null ? " AND cs.start_date <= @to" : "") + @"
            " + (courseCode != null ? " AND cs.sku ILIKE @courseCodePattern" : "") + @"
            " + (trainer != null ? " AND cs.trainer_name ILIKE @trainerPattern" : "") + @"
            " + (type != null ? " AND ((cs.is_public = true AND @type = 'public') OR (cs.is_public = false AND @type = 'private'))" : "") + @"
            " + (status != null ? " AND cs.status = @status" : "");

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            from,
            to,
            courseCodePattern = courseCode != null ? $"%{courseCode}%" : null,
            trainerPattern = trainer != null ? $"%{trainer}%" : null,
            type,
            status
        });
    }

    public async Task<CourseScheduleDetailDto?> GetCourseScheduleByIdAsync(
        long scheduleId,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                cs.id AS Id,
                COALESCE(cs.sku, '') AS CourseCode,
                cs.name AS Title,
                cs.start_date AS StartDate,
                cs.end_date AS EndDate,
                cs.format_type AS Location,
                CASE WHEN cs.is_public THEN 'public' ELSE 'private' END AS Type,
                cs.status AS Status,
                cs.capacity AS Capacity,
                cs.price AS Price,
                cs.sku AS Sku,
                cs.trainer_name AS TrainerName,
                cs.format_type AS FormatType,
                cs.source_system AS SourceSystem,
                cs.source_product_id AS SourceProductId,
                cs.last_synced AS LastSynced,
                COUNT(e.id) AS CurrentEnrolmentCount,
                CASE WHEN COUNT(e.id) >= 3 THEN true ELSE false END AS GuaranteedToRun,
                CASE 
                    WHEN cs.start_date <= CURRENT_DATE + INTERVAL '7 days' 
                         AND COUNT(e.id) < 3 
                    THEN true 
                    ELSE false 
                END AS NeedsAttention
            FROM bagile.course_schedules cs
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id
            WHERE cs.id = @scheduleId
            GROUP BY cs.id, cs.sku, cs.name, cs.start_date, cs.end_date, cs.format_type, 
                     cs.is_public, cs.status, cs.capacity, cs.price, cs.trainer_name, 
                     cs.source_system, cs.source_product_id, cs.last_synced;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<CourseScheduleDetailDto>(sql, new { scheduleId });
    }

    public async Task<IEnumerable<CourseAttendeeDto>> GetCourseAttendeesAsync(
        long scheduleId,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                s.id AS StudentId,
                CONCAT(s.first_name, ' ', s.last_name) AS Name,
                s.email AS Email,
                s.company AS Organisation,
                'Booked' AS Status
            FROM bagile.enrolments e
            JOIN bagile.students s ON e.student_id = s.id
            WHERE e.course_schedule_id = @scheduleId
            ORDER BY s.last_name, s.first_name;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CourseAttendeeDto>(sql, new { scheduleId });
    }
}