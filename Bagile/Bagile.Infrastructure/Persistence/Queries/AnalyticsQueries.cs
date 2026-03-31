using Bagile.Application.Analytics.DTOs;
using Bagile.Application.Common.Interfaces;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class AnalyticsQueries : IAnalyticsQueries
{
    private readonly string _connectionString;

    public AnalyticsQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<OrganisationAnalyticsDto>> GetOrganisationAnalyticsAsync(
        int year, string sortBy = "spend", CancellationToken ct = default)
    {
        var yearStart = new DateTime(year, 1, 1);
        var yearEnd = new DateTime(year, 12, 31);

        var orderByClause = sortBy switch
        {
            "bookings" => "order_count DESC",
            "delegates" => "delegate_count DESC",
            "recency" => "total_spend DESC",
            _ => "total_spend DESC"
        };

        var sql = $@"
            WITH order_companies AS (
                SELECT
                    COALESCE(org.name, o.billing_company) AS company,
                    org.partner_type,
                    org.ptn_tier,
                    org.discount_rate,
                    COUNT(DISTINCT o.id) AS order_count,
                    COUNT(DISTINCT e.student_id) AS delegate_count,
                    COALESCE(SUM(o.net_total), 0) AS total_spend
                FROM bagile.orders o
                LEFT JOIN bagile.organisations org ON (
                    o.billing_company = ANY(org.aliases)
                    OR o.billing_company ILIKE org.name
                )
                LEFT JOIN bagile.enrolments e ON e.order_id = o.id
                    AND e.status NOT IN ('cancelled', 'transferred')
                WHERE o.status = 'completed'
                  AND o.order_date >= @yearStart AND o.order_date <= @yearEnd
                  AND o.billing_company IS NOT NULL AND o.billing_company != ''
                GROUP BY COALESCE(org.name, o.billing_company),
                         org.partner_type, org.ptn_tier, org.discount_rate
            )
            SELECT
                company,
                partner_type AS PartnerType,
                ptn_tier AS PtnTier,
                discount_rate AS DiscountRate,
                order_count AS OrderCount,
                delegate_count AS DelegateCount,
                total_spend AS TotalSpend
            FROM order_companies
            ORDER BY {orderByClause}
            LIMIT 100;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<OrganisationAnalyticsDto>(sql, new { yearStart, yearEnd });
    }

    public async Task<IEnumerable<PartnerAnalyticsDto>> GetPartnerAnalyticsAsync(
        CancellationToken ct = default)
    {
        var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1);

        var sql = @"
            SELECT
                org.name AS Name,
                org.ptn_tier AS PtnTier,
                org.discount_rate AS DiscountRate,
                org.contact_email AS ContactEmail,
                COUNT(DISTINCT o.id) AS BookingsThisYear,
                COUNT(DISTINCT e.student_id) AS DelegatesThisYear,
                COALESCE(SUM(o.net_total), 0) AS SpendThisYear,
                CASE
                    WHEN COUNT(DISTINCT e.student_id) >= 75 THEN 'ptn33'
                    WHEN COUNT(DISTINCT e.student_id) >= 20 THEN 'ptn25'
                    WHEN COUNT(DISTINCT e.student_id) >= 10 THEN 'ptn20'
                    WHEN COUNT(DISTINCT e.student_id) >= 5 THEN 'ptn15'
                    ELSE 'ptn10'
                END AS CalculatedTier,
                CASE
                    WHEN COUNT(DISTINCT e.student_id) >= 75 THEN 33
                    WHEN COUNT(DISTINCT e.student_id) >= 20 THEN 25
                    WHEN COUNT(DISTINCT e.student_id) >= 10 THEN 20
                    WHEN COUNT(DISTINCT e.student_id) >= 5 THEN 15
                    ELSE 10
                END AS CalculatedDiscount
            FROM bagile.organisations org
            LEFT JOIN bagile.orders o ON (
                o.billing_company = ANY(org.aliases)
                AND o.status = 'completed'
                AND o.order_date >= @yearStart
            )
            LEFT JOIN bagile.enrolments e ON e.order_id = o.id
                AND e.status NOT IN ('cancelled', 'transferred')
            WHERE org.partner_type = 'ptn'
            GROUP BY org.id, org.name, org.ptn_tier,
                     org.discount_rate, org.contact_email
            ORDER BY SpendThisYear DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<PartnerAnalyticsDto>(sql, new { yearStart });
    }

    public async Task<IEnumerable<CourseDemandDto>> GetCourseDemandAsync(
        int lookbackMonths = 12, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddMonths(-lookbackMonths);

        var sql = @"
            SELECT
                SPLIT_PART(cs.sku, '-', 1) AS CourseType,
                COUNT(DISTINCT cs.id) AS CoursesRun,
                COUNT(DISTINCT e.id) AS TotalEnrolments,
                ROUND(AVG(sub.fill_count)::numeric, 1) AS AvgAttendees,
                ROUND(
                    AVG(sub.fill_count::numeric / GREATEST(
                        CASE
                            WHEN SPLIT_PART(cs.sku, '-', 1) IN ('PSMA','PSFS','APS','APSSD')
                            THEN 4 ELSE 3
                        END, 1
                    )) * 100, 0
                ) AS AvgFillPct
            FROM bagile.course_schedules cs
            JOIN (
                SELECT course_schedule_id, COUNT(*) AS fill_count
                FROM bagile.enrolments
                WHERE status NOT IN ('cancelled', 'transferred')
                GROUP BY course_schedule_id
            ) sub ON sub.course_schedule_id = cs.id
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id
                AND e.status NOT IN ('cancelled', 'transferred')
            WHERE cs.start_date >= @since
              AND cs.status != 'cancelled'
              AND cs.sku IS NOT NULL
            GROUP BY SPLIT_PART(cs.sku, '-', 1)
            ORDER BY TotalEnrolments DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CourseDemandDto>(sql, new { since });
    }

    public async Task<IEnumerable<CourseDemandMonthlyDto>> GetCourseDemandMonthlyAsync(
        int lookbackMonths = 12, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddMonths(-lookbackMonths);

        var sql = @"
            SELECT
                EXTRACT(YEAR FROM cs.start_date)::int AS Year,
                EXTRACT(MONTH FROM cs.start_date)::int AS Month,
                SPLIT_PART(cs.sku, '-', 1) AS CourseType,
                COUNT(DISTINCT e.id) AS Enrolments
            FROM bagile.course_schedules cs
            JOIN bagile.enrolments e ON e.course_schedule_id = cs.id
                AND e.status NOT IN ('cancelled', 'transferred')
            WHERE cs.start_date >= @since AND cs.sku IS NOT NULL
            GROUP BY EXTRACT(YEAR FROM cs.start_date),
                     EXTRACT(MONTH FROM cs.start_date),
                     SPLIT_PART(cs.sku, '-', 1)
            ORDER BY Year, Month, CourseType;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CourseDemandMonthlyDto>(sql, new { since });
    }

    public async Task<IEnumerable<RepeatCustomerDto>> GetRepeatCustomersAsync(
        int year, int minBookings = 2, CancellationToken ct = default)
    {
        var sql = @"
            WITH company_orders AS (
                SELECT
                    COALESCE(org.name, o.billing_company) AS company,
                    o.id AS order_id,
                    o.order_date,
                    o.net_total,
                    COUNT(DISTINCT e.student_id) AS delegates
                FROM bagile.orders o
                LEFT JOIN bagile.organisations org ON (
                    o.billing_company = ANY(org.aliases)
                    OR o.billing_company ILIKE org.name
                )
                LEFT JOIN bagile.enrolments e ON e.order_id = o.id
                    AND e.status NOT IN ('cancelled', 'transferred')
                WHERE o.status = 'completed'
                  AND o.billing_company IS NOT NULL
                  AND o.billing_company != ''
                GROUP BY COALESCE(org.name, o.billing_company),
                         o.id, o.order_date, o.net_total
            )
            SELECT
                company AS Company,
                COUNT(*)::int AS TotalBookings,
                COALESCE(SUM(net_total), 0) AS LifetimeSpend,
                COALESCE(SUM(delegates), 0)::int AS LifetimeDelegates,
                MIN(order_date) AS FirstBooking,
                MAX(order_date) AS LastBooking,
                EXTRACT(DAY FROM MAX(order_date) - MIN(order_date))::int
                    AS RelationshipDays,
                COUNT(*) FILTER (
                    WHERE EXTRACT(YEAR FROM order_date) = @year
                )::int AS BookingsThisYear,
                COALESCE(SUM(net_total) FILTER (
                    WHERE EXTRACT(YEAR FROM order_date) = @year
                ), 0) AS SpendThisYear
            FROM company_orders
            GROUP BY company
            HAVING COUNT(*) >= @minBookings
            ORDER BY TotalBookings DESC, LifetimeSpend DESC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<RepeatCustomerDto>(sql, new { year, minBookings });
    }
}
