using Bagile.Application.Common.Interfaces;
using Bagile.Application.Organisations.DTOs;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class OrganisationQueries : IOrganisationQueries
{
    private readonly string _connectionString;

    public OrganisationQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<OrganisationDto>> GetOrganisationsAsync(
        string? name,
        string? domain,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = @"
            WITH org_names AS (
                -- Get unique organisation names from orders (billing_company)
                SELECT DISTINCT 
                    o.billing_company AS org_name
                FROM bagile.orders o
                WHERE o.billing_company IS NOT NULL 
                    AND TRIM(o.billing_company) != ''
                
                UNION
                
                -- Get unique organisation names from students (company)
                SELECT DISTINCT 
                    s.company AS org_name
                FROM bagile.students s
                WHERE s.company IS NOT NULL 
                    AND TRIM(s.company) != ''
            ),
            org_stats AS (
                SELECT 
                    COALESCE(s.company, o.billing_company) AS org_name,
                    COUNT(DISTINCT s.id) AS total_students,
                    COUNT(DISTINCT e.id) AS total_enrolments,
                    STRING_AGG(DISTINCT SUBSTRING(s.email FROM '@(.*)$'), ', ') AS domains
                FROM bagile.students s
                LEFT JOIN bagile.enrolments e ON e.student_id = s.id
                LEFT JOIN bagile.orders o ON e.order_id = o.id
                WHERE COALESCE(s.company, o.billing_company) IS NOT NULL
                GROUP BY COALESCE(s.company, o.billing_company)
            )
            SELECT 
                os.org_name AS Name,
                SPLIT_PART(os.domains, ', ', 1) AS PrimaryDomain,
                os.total_students AS TotalStudents,
                os.total_enrolments AS TotalEnrolments
            FROM org_stats os
            WHERE 1=1
            " + (name != null ? " AND os.org_name ILIKE @namePattern" : "") + @"
            " + (domain != null ? " AND os.domains ILIKE @domainPattern" : "") + @"
            ORDER BY os.total_enrolments DESC, os.org_name
            LIMIT @pageSize OFFSET @offset;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<OrganisationDto>(sql, new
        {
            namePattern = name != null ? $"%{name}%" : null,
            domainPattern = domain != null ? $"%{domain}%" : null,
            pageSize,
            offset = (page - 1) * pageSize
        });
    }

    public async Task<int> CountOrganisationsAsync(
        string? name,
        string? domain,
        CancellationToken ct = default)
    {
        var sql = @"
            WITH org_names AS (
                SELECT DISTINCT 
                    o.billing_company AS org_name
                FROM bagile.orders o
                WHERE o.billing_company IS NOT NULL 
                    AND TRIM(o.billing_company) != ''
                
                UNION
                
                SELECT DISTINCT 
                    s.company AS org_name
                FROM bagile.students s
                WHERE s.company IS NOT NULL 
                    AND TRIM(s.company) != ''
            ),
            org_stats AS (
                SELECT 
                    COALESCE(s.company, o.billing_company) AS org_name,
                    STRING_AGG(DISTINCT SUBSTRING(s.email FROM '@(.*)$'), ', ') AS domains
                FROM bagile.students s
                LEFT JOIN bagile.enrolments e ON e.student_id = s.id
                LEFT JOIN bagile.orders o ON e.order_id = o.id
                WHERE COALESCE(s.company, o.billing_company) IS NOT NULL
                GROUP BY COALESCE(s.company, o.billing_company)
            )
            SELECT COUNT(*)
            FROM org_stats os
            WHERE 1=1
            " + (name != null ? " AND os.org_name ILIKE @namePattern" : "") + @"
            " + (domain != null ? " AND os.domains ILIKE @domainPattern" : "");

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            namePattern = name != null ? $"%{name}%" : null,
            domainPattern = domain != null ? $"%{domain}%" : null
        });
    }

    public async Task<OrganisationDetailDto?> GetOrganisationByNameAsync(
        string name,
        CancellationToken ct = default)
    {
        var sql = @"
            WITH org_stats AS (
                SELECT 
                    COALESCE(s.company, o.billing_company) AS org_name,
                    COUNT(DISTINCT s.id) AS total_students,
                    COUNT(DISTINCT e.id) AS total_enrolments,
                    COUNT(DISTINCT o.id) AS total_orders,
                    COALESCE(SUM(o.total_amount), 0) AS total_revenue,
                    MIN(o.order_date) AS first_order_date,
                    MAX(o.order_date) AS last_order_date,
                    MAX(cs.start_date) AS last_course_date,
                    STRING_AGG(DISTINCT SUBSTRING(s.email FROM '@(.*)$'), ', ') AS domains
                FROM bagile.students s
                LEFT JOIN bagile.enrolments e ON e.student_id = s.id
                LEFT JOIN bagile.orders o ON e.order_id = o.id
                LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
                WHERE COALESCE(s.company, o.billing_company) = @name
                GROUP BY COALESCE(s.company, o.billing_company)
            )
            SELECT 
                os.org_name AS Name,
                SPLIT_PART(os.domains, ', ', 1) AS PrimaryDomain,
                os.total_students AS TotalStudents,
                os.total_enrolments AS TotalEnrolments,
                os.total_orders AS TotalOrders,
                os.total_revenue AS TotalRevenue,
                os.first_order_date AS FirstOrderDate,
                os.last_order_date AS LastOrderDate,
                os.last_course_date AS LastCourseDate
            FROM org_stats os;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<OrganisationDetailDto>(sql, new { name });
    }

    public async Task<IEnumerable<OrganisationCourseHistoryDto>> GetOrganisationCourseHistoryAsync(
        string organisationName,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                COALESCE(cs.sku, 'PRIVATE') AS CourseCode,
                COALESCE(cs.name, 'Private Course') AS CourseTitle,
                COUNT(CASE WHEN cs.is_public = true THEN 1 END) AS PublicCount,
                COUNT(CASE WHEN cs.is_public = false OR cs.is_public IS NULL THEN 1 END) AS PrivateCount,
                COUNT(*) AS TotalCount,
                MAX(cs.start_date) AS LastRunDate
            FROM bagile.enrolments e
            JOIN bagile.students s ON e.student_id = s.id
            LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            LEFT JOIN bagile.orders o ON e.order_id = o.id
            WHERE COALESCE(s.company, o.billing_company) = @organisationName
            GROUP BY COALESCE(cs.sku, 'PRIVATE'), COALESCE(cs.name, 'Private Course')
            ORDER BY TotalCount DESC, LastRunDate DESC NULLS LAST;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<OrganisationCourseHistoryDto>(sql, new { organisationName });
    }
}