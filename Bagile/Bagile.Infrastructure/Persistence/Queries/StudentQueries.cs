using Bagile.Application.Common.Interfaces;
using Bagile.Application.Students.DTOs;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class StudentQueries : IStudentQueries
{
    private readonly string _connectionString;

    public StudentQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<StudentDto>> GetStudentsAsync(
        string? email,
        string? name,
        string? organisation,
        string? courseCode,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT DISTINCT
                s.id AS Id,
                s.email AS Email,
                s.first_name AS FirstName,
                s.last_name AS LastName,
                CONCAT(s.first_name, ' ', s.last_name) AS FullName,
                s.company AS Company,
                s.created_at AS CreatedAt
            FROM bagile.students s
            " + (courseCode != null ? @"
            JOIN bagile.enrolments e ON e.student_id = s.id
            JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            " : "") + @"
            WHERE 1=1
            " + (email != null ? " AND s.email ILIKE @emailPattern" : "") + @"
            " + (name != null ? " AND (s.first_name ILIKE @namePattern OR s.last_name ILIKE @namePattern)" : "") + @"
            " + (organisation != null ? " AND s.company ILIKE @organisationPattern" : "") + @"
            " + (courseCode != null ? " AND cs.sku ILIKE @courseCodePattern" : "") + @"
            ORDER BY s.last_name, s.first_name
            LIMIT @pageSize OFFSET @offset;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<StudentDto>(sql, new
        {
            emailPattern = email != null ? $"%{email}%" : null,
            namePattern = name != null ? $"%{name}%" : null,
            organisationPattern = organisation != null ? $"%{organisation}%" : null,
            courseCodePattern = courseCode != null ? $"%{courseCode}%" : null,
            pageSize,
            offset = (page - 1) * pageSize
        });
    }

    public async Task<int> CountStudentsAsync(
        string? email,
        string? name,
        string? organisation,
        string? courseCode,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT COUNT(DISTINCT s.id)
            FROM bagile.students s
            " + (courseCode != null ? @"
            JOIN bagile.enrolments e ON e.student_id = s.id
            JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            " : "") + @"
            WHERE 1=1
            " + (email != null ? " AND s.email ILIKE @emailPattern" : "") + @"
            " + (name != null ? " AND (s.first_name ILIKE @namePattern OR s.last_name ILIKE @namePattern)" : "") + @"
            " + (organisation != null ? " AND s.company ILIKE @organisationPattern" : "") + @"
            " + (courseCode != null ? " AND cs.sku ILIKE @courseCodePattern" : "");

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            emailPattern = email != null ? $"%{email}%" : null,
            namePattern = name != null ? $"%{name}%" : null,
            organisationPattern = organisation != null ? $"%{organisation}%" : null,
            courseCodePattern = courseCode != null ? $"%{courseCode}%" : null
        });
    }

    public async Task<StudentDetailDto?> GetStudentByIdAsync(
        long studentId,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                s.id AS Id,
                s.email AS Email,
                s.first_name AS FirstName,
                s.last_name AS LastName,
                CONCAT(s.first_name, ' ', s.last_name) AS FullName,
                s.company AS Company,
                s.created_at AS CreatedAt,
                s.updated_at AS UpdatedAt,
                COUNT(e.id) AS TotalEnrolments,
                MAX(cs.start_date) AS LastCourseDate
            FROM bagile.students s
            LEFT JOIN bagile.enrolments e ON e.student_id = s.id
            LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            WHERE s.id = @studentId
            GROUP BY s.id, s.email, s.first_name, s.last_name, s.company, s.created_at, s.updated_at;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<StudentDetailDto>(sql, new { studentId });
    }

    public async Task<IEnumerable<StudentEnrolmentDto>> GetStudentEnrolmentsAsync(
        long studentId,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                e.id AS EnrolmentId,
                COALESCE(e.course_schedule_id, 0) AS CourseScheduleId,
                COALESCE(cs.sku, 'PRIVATE') AS CourseCode,
                COALESCE(cs.name, 'Private Course') AS CourseTitle,
                cs.start_date AS CourseStartDate,
                'Booked' AS Status,
                CASE WHEN cs.is_public THEN 'public' ELSE 'private' END AS Type,
                e.created_at AS EnrolledAt
            FROM bagile.enrolments e
            LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            WHERE e.student_id = @studentId
            ORDER BY cs.start_date DESC NULLS LAST, e.created_at DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<StudentEnrolmentDto>(sql, new { studentId });
    }
}