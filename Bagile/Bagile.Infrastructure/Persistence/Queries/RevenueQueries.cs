using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Common.Interfaces;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class RevenueQueries : IRevenueQueries
{
    private readonly string _connectionString;

    public RevenueQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<MonthlyRevenueDto>> GetMonthlyRevenueAsync(
        int year, CancellationToken ct = default)
    {
        var sql = @"
            SELECT
                EXTRACT(YEAR FROM o.order_date)::int AS Year,
                EXTRACT(MONTH FROM o.order_date)::int AS Month,
                TO_CHAR(o.order_date, 'Mon') AS MonthName,
                COALESCE(SUM(o.net_total), 0) AS Revenue,
                COUNT(DISTINCT o.id) AS OrderCount,
                COALESCE(SUM(o.total_quantity), 0) AS AttendeeCount
            FROM bagile.orders o
            WHERE EXTRACT(YEAR FROM o.order_date) = @year
              AND o.status NOT IN ('cancelled', 'refunded', 'failed')
            GROUP BY
                EXTRACT(YEAR FROM o.order_date),
                EXTRACT(MONTH FROM o.order_date),
                TO_CHAR(o.order_date, 'Mon')
            ORDER BY Month;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<MonthlyRevenueDto>(sql, new { year });
    }

    public async Task<IEnumerable<CourseTypeRevenueDto>> GetRevenueByCourseTypeAsync(
        int year, CancellationToken ct = default)
    {
        var sql = @"
            WITH order_active_counts AS (
                SELECT order_id, COUNT(*) AS active_count
                FROM bagile.enrolments
                WHERE status != 'cancelled'
                GROUP BY order_id
            ),
            enrolment_share AS (
                SELECT
                    e.id AS enrolment_id,
                    e.order_id,
                    e.course_schedule_id,
                    o.net_total / NULLIF(oac.active_count, 0) AS share
                FROM bagile.enrolments e
                JOIN bagile.orders o ON e.order_id = o.id
                JOIN order_active_counts oac ON oac.order_id = e.order_id
                WHERE EXTRACT(YEAR FROM o.order_date) = @year
                  AND o.status NOT IN ('cancelled', 'refunded', 'failed')
                  AND e.status != 'cancelled'
            )
            SELECT
                COALESCE(
                    CASE
                        WHEN cs.sku ~ '^[A-Z]+-' THEN SPLIT_PART(cs.sku, '-', 1)
                        ELSE 'Other'
                    END,
                    'Other'
                ) AS CourseType,
                COALESCE(SUM(es.share), 0) AS Revenue,
                COUNT(DISTINCT es.order_id) AS OrderCount,
                COUNT(es.enrolment_id) AS AttendeeCount
            FROM enrolment_share es
            JOIN bagile.course_schedules cs ON es.course_schedule_id = cs.id
            GROUP BY CourseType
            ORDER BY Revenue DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CourseTypeRevenueDto>(sql, new { year });
    }

    public async Task<IEnumerable<MonthlyRevenueDetailDto>> GetMonthDrilldownAsync(
        int year, int month, CancellationToken ct = default)
    {
        var sql = @"
            SELECT
                o.id AS OrderId,
                o.external_id AS ExternalId,
                o.order_date AS OrderDate,
                o.billing_company AS Company,
                o.contact_name AS ContactName,
                o.contact_email AS ContactEmail,
                COALESCE(o.net_total, 0) AS NetRevenue,
                COALESCE(o.total_amount, 0) AS GrossRevenue,
                COALESCE(o.refund_total, 0) AS RefundAmount,
                o.lifecycle_status AS LifecycleStatus,
                COALESCE(o.payment_method_title, o.payment_method) AS PaymentMethod,
                cs.sku AS CourseCode,
                cs.name AS CourseName,
                cs.start_date AS CourseDate,
                CASE WHEN cs.is_public THEN 'public' ELSE 'private' END AS CourseType,
                COUNT(e.id)::int AS AttendeeCount
            FROM bagile.orders o
            LEFT JOIN bagile.enrolments e ON e.order_id = o.id
                AND e.status NOT IN ('cancelled', 'transferred')
            LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            WHERE EXTRACT(YEAR FROM o.order_date) = @year
              AND EXTRACT(MONTH FROM o.order_date) = @month
              AND o.status NOT IN ('cancelled', 'refunded', 'failed')
            GROUP BY o.id, cs.id
            ORDER BY o.order_date DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<MonthlyRevenueDetailDto>(sql, new { year, month });
    }

    public async Task<IEnumerable<SourceRevenueDto>> GetRevenueBySourceAsync(
        int year, CancellationToken ct = default)
    {
        var sql = @"
            WITH order_active_counts AS (
                SELECT order_id, COUNT(*) AS active_count
                FROM bagile.enrolments
                WHERE status != 'cancelled'
                GROUP BY order_id
            ),
            enrolment_share AS (
                SELECT
                    e.id AS enrolment_id,
                    e.order_id,
                    e.course_schedule_id,
                    o.net_total / NULLIF(oac.active_count, 0) AS share
                FROM bagile.enrolments e
                JOIN bagile.orders o ON e.order_id = o.id
                JOIN order_active_counts oac ON oac.order_id = e.order_id
                WHERE EXTRACT(YEAR FROM o.order_date) = @year
                  AND o.status NOT IN ('cancelled', 'refunded', 'failed')
                  AND e.status != 'cancelled'
            )
            SELECT
                CASE WHEN cs.is_public THEN 'public' ELSE 'private' END AS Source,
                COALESCE(SUM(es.share), 0) AS Revenue,
                COUNT(DISTINCT es.order_id) AS OrderCount,
                COUNT(es.enrolment_id) AS AttendeeCount
            FROM enrolment_share es
            JOIN bagile.course_schedules cs ON es.course_schedule_id = cs.id
            GROUP BY CASE WHEN cs.is_public THEN 'public' ELSE 'private' END
            ORDER BY Revenue DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<SourceRevenueDto>(sql, new { year });
    }

    public async Task<IEnumerable<CountryRevenueDto>> GetRevenueByCountryAsync(
        int year, CancellationToken ct = default)
    {
        var sql = @"
            WITH order_active_counts AS (
                SELECT order_id, COUNT(*) AS active_count
                FROM bagile.enrolments
                WHERE status != 'cancelled'
                GROUP BY order_id
            ),
            enrolment_share AS (
                SELECT
                    e.id AS enrolment_id,
                    e.order_id,
                    e.student_id,
                    o.net_total / NULLIF(oac.active_count, 0) AS share
                FROM bagile.enrolments e
                JOIN bagile.orders o ON e.order_id = o.id
                JOIN order_active_counts oac ON oac.order_id = e.order_id
                WHERE EXTRACT(YEAR FROM o.order_date) = @year
                  AND o.status NOT IN ('cancelled', 'refunded', 'failed')
                  AND e.status != 'cancelled'
            )
            SELECT
                COALESCE(s.country, 'Unknown') AS Country,
                CASE
                    WHEN UPPER(COALESCE(s.country, '')) = 'GB' THEN 'UK'
                    WHEN UPPER(COALESCE(s.country, '')) IN (
                        'AT','BE','BG','HR','CY','CZ','DK','EE','FI','FR','DE',
                        'GR','HU','IE','IT','LV','LT','LU','MT','NL','PL','PT',
                        'RO','SK','SI','ES','SE','NO','CH','IS'
                    ) THEN 'Europe'
                    WHEN COALESCE(s.country, '') = '' THEN 'Unknown'
                    ELSE 'Rest of World'
                END AS Region,
                COALESCE(SUM(es.share), 0) AS Revenue,
                COUNT(DISTINCT es.order_id) AS OrderCount,
                COUNT(es.enrolment_id) AS AttendeeCount
            FROM enrolment_share es
            JOIN bagile.students s ON es.student_id = s.id
            GROUP BY Country, Region
            ORDER BY Revenue DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CountryRevenueDto>(sql, new { year });
    }
}
