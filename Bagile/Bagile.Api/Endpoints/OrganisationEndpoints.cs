using Dapper;
using Npgsql;

namespace Bagile.Api.Endpoints;

public static class OrganisationEndpoints
{
    public static void MapOrganisationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/analytics/organisations", GetOrganisationAnalytics);
        app.MapGet("/api/analytics/partners", GetPartnerAnalytics);
        app.MapGet("/api/analytics/course-demand", GetCourseDemand);
    }

    private static async Task<IResult> GetOrganisationAnalytics(
        IConfiguration config,
        int? year = null)
    {
        var connStr = GetConnStr(config);
        var targetYear = year ?? DateTime.UtcNow.Year;
        var yearStart = new DateTime(targetYear, 1, 1);
        var yearEnd = new DateTime(targetYear, 12, 31);

        await using var conn = new NpgsqlConnection(connStr);

        // Get top companies by spend with fuzzy matching via organisations table
        var orgs = await conn.QueryAsync<dynamic>(@"
            WITH order_companies AS (
                SELECT
                    COALESCE(org.name, o.billing_company) AS company,
                    org.partner_type,
                    org.ptn_tier,
                    org.discount_rate,
                    COUNT(DISTINCT o.id) AS order_count,
                    COUNT(DISTINCT e.student_id) AS delegate_count,
                    COALESCE(SUM(o.total_amount), 0) AS total_spend
                FROM bagile.orders o
                LEFT JOIN bagile.organisations org ON (
                    o.billing_company = ANY(org.aliases)
                    OR o.billing_company ILIKE org.name
                )
                LEFT JOIN bagile.enrolments e ON e.order_id = o.id AND e.status NOT IN ('cancelled', 'transferred')
                WHERE o.status = 'completed'
                  AND o.order_date >= @yearStart AND o.order_date <= @yearEnd
                  AND o.billing_company IS NOT NULL AND o.billing_company != ''
                GROUP BY COALESCE(org.name, o.billing_company), org.partner_type, org.ptn_tier, org.discount_rate
            )
            SELECT * FROM order_companies
            ORDER BY total_spend DESC
            LIMIT 50
        ", new { yearStart, yearEnd });

        return Results.Ok(new { year = targetYear, organisations = orgs });
    }

    private static async Task<IResult> GetPartnerAnalytics(IConfiguration config)
    {
        var connStr = GetConnStr(config);
        var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1);

        await using var conn = new NpgsqlConnection(connStr);

        var partners = await conn.QueryAsync<dynamic>(@"
            SELECT
                org.name,
                org.ptn_tier,
                org.discount_rate,
                org.contact_email,
                COUNT(DISTINCT o.id) AS bookings_this_year,
                COUNT(DISTINCT e.student_id) AS delegates_this_year,
                COALESCE(SUM(o.total_amount), 0) AS spend_this_year,
                CASE
                    WHEN COUNT(DISTINCT e.student_id) >= 75 THEN 'ptn33'
                    WHEN COUNT(DISTINCT e.student_id) >= 20 THEN 'ptn25'
                    WHEN COUNT(DISTINCT e.student_id) >= 10 THEN 'ptn20'
                    WHEN COUNT(DISTINCT e.student_id) >= 5 THEN 'ptn15'
                    ELSE 'ptn10'
                END AS calculated_tier,
                CASE
                    WHEN COUNT(DISTINCT e.student_id) >= 75 THEN 33
                    WHEN COUNT(DISTINCT e.student_id) >= 20 THEN 25
                    WHEN COUNT(DISTINCT e.student_id) >= 10 THEN 20
                    WHEN COUNT(DISTINCT e.student_id) >= 5 THEN 15
                    ELSE 10
                END AS calculated_discount
            FROM bagile.organisations org
            LEFT JOIN bagile.orders o ON (
                o.billing_company = ANY(org.aliases) AND o.status = 'completed'
                AND o.order_date >= @yearStart
            )
            LEFT JOIN bagile.enrolments e ON e.order_id = o.id AND e.status NOT IN ('cancelled', 'transferred')
            WHERE org.partner_type = 'ptn'
            GROUP BY org.id, org.name, org.ptn_tier, org.discount_rate, org.contact_email
            ORDER BY spend_this_year DESC
        ", new { yearStart });

        return Results.Ok(partners);
    }

    private static async Task<IResult> GetCourseDemand(
        IConfiguration config,
        int? months = null)
    {
        var connStr = GetConnStr(config);
        var lookback = months ?? 12;
        var since = DateTime.UtcNow.AddMonths(-lookback);

        await using var conn = new NpgsqlConnection(connStr);

        var demand = await conn.QueryAsync<dynamic>(@"
            SELECT
                SPLIT_PART(cs.sku, '-', 1) AS course_type,
                COUNT(DISTINCT cs.id) AS courses_run,
                COUNT(DISTINCT e.id) AS total_enrolments,
                ROUND(AVG(sub.fill_count)::numeric, 1) AS avg_attendees,
                ROUND(
                    AVG(sub.fill_count::numeric / GREATEST(
                        CASE WHEN SPLIT_PART(cs.sku, '-', 1) IN ('PSMA','PSFS','APS','APSSD') THEN 4 ELSE 3 END,
                        1
                    )) * 100, 0
                ) AS avg_fill_pct
            FROM bagile.course_schedules cs
            JOIN (
                SELECT course_schedule_id, COUNT(*) AS fill_count
                FROM bagile.enrolments
                WHERE status NOT IN ('cancelled', 'transferred')
                GROUP BY course_schedule_id
            ) sub ON sub.course_schedule_id = cs.id
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id AND e.status NOT IN ('cancelled', 'transferred')
            WHERE cs.start_date >= @since
              AND cs.status != 'cancelled'
              AND cs.sku IS NOT NULL
            GROUP BY SPLIT_PART(cs.sku, '-', 1)
            ORDER BY total_enrolments DESC
        ", new { since });

        // Monthly trend
        var monthly = await conn.QueryAsync<dynamic>(@"
            SELECT
                EXTRACT(YEAR FROM cs.start_date)::int AS year,
                EXTRACT(MONTH FROM cs.start_date)::int AS month,
                SPLIT_PART(cs.sku, '-', 1) AS course_type,
                COUNT(DISTINCT e.id) AS enrolments
            FROM bagile.course_schedules cs
            JOIN bagile.enrolments e ON e.course_schedule_id = cs.id AND e.status NOT IN ('cancelled', 'transferred')
            WHERE cs.start_date >= @since AND cs.sku IS NOT NULL
            GROUP BY EXTRACT(YEAR FROM cs.start_date), EXTRACT(MONTH FROM cs.start_date), SPLIT_PART(cs.sku, '-', 1)
            ORDER BY year, month, course_type
        ", new { since });

        return Results.Ok(new { lookbackMonths = lookback, courseTypes = demand, monthlyTrend = monthly });
    }

    private static string GetConnStr(IConfiguration config) =>
        config.GetConnectionString("DefaultConnection")
        ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not found");
}
