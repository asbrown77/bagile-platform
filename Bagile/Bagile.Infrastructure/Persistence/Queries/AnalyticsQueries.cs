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
        int? year = null, string sortBy = "spend", CancellationToken ct = default)
    {
        var yearStart = year.HasValue ? new DateTime(year.Value, 1, 1) : (DateTime?)null;
        var yearEnd   = year.HasValue ? new DateTime(year.Value, 12, 31, 23, 59, 59) : (DateTime?)null;

        var orderByClause = sortBy switch
        {
            "bookings" => "order_count DESC",
            "delegates" => "delegate_count DESC",
            "recency" => "total_spend DESC",
            _ => "total_spend DESC"
        };

        var yearFilter = year.HasValue
            ? "AND o.order_date >= @yearStart AND o.order_date <= @yearEnd"
            : "";

        var sql = $@"
            WITH orders_resolved AS (
                -- One row per order; resolve billing_company to canonical org name
                SELECT
                    o.id AS order_id,
                    COALESCE(org.name, o.billing_company) AS company,
                    org.partner_type,
                    org.ptn_tier,
                    org.discount_rate,
                    o.net_total
                FROM bagile.orders o
                LEFT JOIN bagile.organisations org ON (
                    LOWER(TRIM(o.billing_company)) = ANY(
                        SELECT LOWER(TRIM(a)) FROM UNNEST(org.aliases) a
                    )
                )
                WHERE o.status = 'completed'
                  {yearFilter}
                  AND o.billing_company IS NOT NULL AND o.billing_company != ''
            ),
            order_stats AS (
                -- Revenue and order count per company — no enrolment join, so no fanout
                SELECT
                    company,
                    partner_type,
                    ptn_tier,
                    discount_rate,
                    COUNT(DISTINCT order_id) AS order_count,
                    COALESCE(SUM(net_total), 0) AS total_spend
                FROM orders_resolved
                GROUP BY company, partner_type, ptn_tier, discount_rate
            ),
            delegate_stats AS (
                -- Delegate count per company — separate enrolment join
                SELECT
                    r.company,
                    COUNT(DISTINCT e.student_id) AS delegate_count
                FROM orders_resolved r
                JOIN bagile.enrolments e ON e.order_id = r.order_id
                    AND e.status NOT IN ('cancelled', 'transferred')
                GROUP BY r.company
            )
            SELECT
                os.company AS company,
                os.partner_type AS PartnerType,
                os.ptn_tier AS PtnTier,
                os.discount_rate AS DiscountRate,
                os.order_count AS OrderCount,
                COALESCE(ds.delegate_count, 0) AS DelegateCount,
                os.total_spend AS TotalSpend
            FROM order_stats os
            LEFT JOIN delegate_stats ds ON ds.company = os.company
            ORDER BY {orderByClause}
            LIMIT 100;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<OrganisationAnalyticsDto>(sql, new { yearStart, yearEnd });
    }

    public async Task<IEnumerable<PartnerAnalyticsDto>> GetPartnerAnalyticsAsync(
        int? year = null, CancellationToken ct = default)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;
        var yearStart = new DateTime(targetYear, 1, 1);

        var sql = @"
            WITH order_spend AS (
                -- Revenue per partner org — no enrolment join, so no fanout
                SELECT
                    org.id AS org_id,
                    COUNT(DISTINCT o.id) AS order_count,
                    COALESCE(SUM(o.net_total), 0) AS total_spend
                FROM bagile.organisations org
                LEFT JOIN bagile.orders o ON (
                    o.billing_company = ANY(org.aliases)
                    AND o.status = 'completed'
                    AND o.order_date >= @yearStart
                )
                WHERE org.partner_type = 'ptn'
                GROUP BY org.id
            ),
            delegate_count AS (
                -- Delegate count per partner org — separate enrolment join
                SELECT
                    org.id AS org_id,
                    COUNT(DISTINCT e.student_id) AS delegate_count
                FROM bagile.organisations org
                LEFT JOIN bagile.orders o ON (
                    o.billing_company = ANY(org.aliases)
                    AND o.status = 'completed'
                    AND o.order_date >= @yearStart
                )
                LEFT JOIN bagile.enrolments e ON e.order_id = o.id
                    AND e.status NOT IN ('cancelled', 'transferred')
                WHERE org.partner_type = 'ptn'
                GROUP BY org.id
            )
            SELECT
                org.name AS Name,
                org.ptn_tier AS PtnTier,
                org.discount_rate AS DiscountRate,
                org.contact_email AS ContactEmail,
                os.order_count AS BookingsThisYear,
                COALESCE(dc.delegate_count, 0) AS DelegatesThisYear,
                os.total_spend AS SpendThisYear,
                CASE
                    WHEN COALESCE(dc.delegate_count, 0) >= 75 THEN 'ptn33'
                    WHEN COALESCE(dc.delegate_count, 0) >= 20 THEN 'ptn25'
                    WHEN COALESCE(dc.delegate_count, 0) >= 10 THEN 'ptn20'
                    WHEN COALESCE(dc.delegate_count, 0) >= 5 THEN 'ptn15'
                    ELSE 'ptn10'
                END AS CalculatedTier,
                CASE
                    WHEN COALESCE(dc.delegate_count, 0) >= 75 THEN 33
                    WHEN COALESCE(dc.delegate_count, 0) >= 20 THEN 25
                    WHEN COALESCE(dc.delegate_count, 0) >= 10 THEN 20
                    WHEN COALESCE(dc.delegate_count, 0) >= 5 THEN 15
                    ELSE 10
                END AS CalculatedDiscount
            FROM bagile.organisations org
            JOIN order_spend os ON os.org_id = org.id
            LEFT JOIN delegate_count dc ON dc.org_id = org.id
            WHERE org.partner_type = 'ptn'
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
                    LOWER(TRIM(o.billing_company)) = ANY(
                        SELECT LOWER(TRIM(a)) FROM UNNEST(org.aliases) a
                    )
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
