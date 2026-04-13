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

    public async Task<IEnumerable<OrganisationSummaryDto>> SearchOrganisationsAsync(
        string q,
        CancellationToken ct = default)
    {
        // Match on name OR any alias. Aliases are a TEXT[] column.
        // We use ILIKE for partial matching on name and array_to_string for aliases.
        const string sql = @"
            SELECT id        AS Id,
                   name      AS Name,
                   acronym   AS Acronym,
                   partner_type AS PartnerType,
                   ptn_tier  AS PtnTier
            FROM bagile.organisations
            WHERE name ILIKE @pattern
               OR array_to_string(aliases, ' ') ILIKE @pattern
            ORDER BY
                -- Exact-prefix matches first
                CASE WHEN name ILIKE @prefix THEN 0 ELSE 1 END,
                name
            LIMIT 10;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<OrganisationSummaryDto>(sql, new
        {
            pattern = $"%{q}%",
            prefix  = $"{q}%",
        });
    }

    /// <summary>
    /// CTE fragment that resolves raw company names to canonical organisation names.
    /// LEFT JOINs against bagile.organisations using case-insensitive alias matching.
    /// Unknown orgs (no match) fall back to the raw company value.
    /// Blank/null company values are excluded.
    /// </summary>
    private const string NormalisedCompanyCte = @"
        -- Gather all company name occurrences from students and orders
        raw_companies AS (
            SELECT COALESCE(o.billing_company, s.company) AS company_value,
                   s.id AS student_id,
                   s.email AS student_email,
                   e.id AS enrolment_id,
                   o.id AS order_id
            FROM bagile.students s
            LEFT JOIN bagile.enrolments e ON e.student_id = s.id
                AND e.status NOT IN ('cancelled', 'transferred')
            LEFT JOIN bagile.orders o ON e.order_id = o.id
            WHERE COALESCE(o.billing_company, s.company) IS NOT NULL
              AND NULLIF(TRIM(COALESCE(o.billing_company, s.company)), '') IS NOT NULL
        ),
        -- Resolve each company value to its canonical org name via alias matching
        resolved AS (
            SELECT rc.*,
                   COALESCE(org.name, rc.company_value) AS canonical_name,
                   org.primary_domain AS org_primary_domain
            FROM raw_companies rc
            LEFT JOIN bagile.organisations org
                   ON LOWER(TRIM(rc.company_value)) = ANY(
                       SELECT LOWER(TRIM(a)) FROM UNNEST(org.aliases) a
                   )
        )";

    public async Task<IEnumerable<OrganisationDto>> GetOrganisationsAsync(
        string? name,
        string? domain,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = @"
            WITH " + NormalisedCompanyCte + @",
            org_stats AS (
                SELECT
                    r.canonical_name AS org_name,
                    COALESCE(
                        MAX(r.org_primary_domain),
                        SPLIT_PART(
                            STRING_AGG(DISTINCT SUBSTRING(r.student_email FROM '@(.*)$'), ', '),
                            ', ', 1
                        )
                    ) AS primary_domain,
                    COUNT(DISTINCT r.student_id) AS total_students,
                    COUNT(DISTINCT r.enrolment_id) AS total_enrolments
                FROM resolved r
                GROUP BY r.canonical_name
            )
            SELECT
                os.org_name AS Name,
                os.primary_domain AS PrimaryDomain,
                os.total_students AS TotalStudents,
                os.total_enrolments AS TotalEnrolments
            FROM org_stats os
            WHERE 1=1
            " + (name != null ? " AND os.org_name ILIKE @namePattern" : "") + @"
            " + (domain != null ? " AND os.primary_domain ILIKE @domainPattern" : "") + @"
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
            WITH " + NormalisedCompanyCte + @",
            org_stats AS (
                SELECT
                    r.canonical_name AS org_name,
                    COALESCE(
                        MAX(r.org_primary_domain),
                        SPLIT_PART(
                            STRING_AGG(DISTINCT SUBSTRING(r.student_email FROM '@(.*)$'), ', '),
                            ', ', 1
                        )
                    ) AS primary_domain
                FROM resolved r
                GROUP BY r.canonical_name
            )
            SELECT COUNT(*)
            FROM org_stats os
            WHERE 1=1
            " + (name != null ? " AND os.org_name ILIKE @namePattern" : "") + @"
            " + (domain != null ? " AND os.primary_domain ILIKE @domainPattern" : "");

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
        // Match by canonical org name OR by alias lookup.
        // This ensures "NobleProg" finds all bookings across all billing_company variations.
        var sql = @"
            WITH alias_matches AS (
                -- All company values that map to this org (via aliases or exact name match)
                SELECT unnest(org.aliases) AS alias_value
                FROM bagile.organisations org
                WHERE org.name ILIKE @name
                   OR LOWER(TRIM(@name)) = ANY(
                       SELECT LOWER(TRIM(a)) FROM UNNEST(org.aliases) a
                   )

                UNION

                SELECT @name AS alias_value
            ),
            org_stats AS (
                SELECT
                    COALESCE(
                        (SELECT org.name FROM bagile.organisations org
                         WHERE org.name ILIKE @name
                            OR LOWER(TRIM(@name)) = ANY(
                                SELECT LOWER(TRIM(a)) FROM UNNEST(org.aliases) a
                            )
                         LIMIT 1),
                        @name
                    ) AS org_name,
                    COUNT(DISTINCT s.id) AS total_students,
                    COUNT(DISTINCT e.id) AS total_enrolments,
                    COUNT(DISTINCT o.id) AS total_orders,
                    COALESCE(SUM(o.net_total), 0) AS total_revenue,
                    MIN(o.order_date) AS first_order_date,
                    MAX(o.order_date) AS last_order_date,
                    MAX(cs.start_date) AS last_course_date,
                    STRING_AGG(DISTINCT SUBSTRING(s.email FROM '@(.*)$'), ', ') AS domains
                FROM bagile.students s
                LEFT JOIN bagile.enrolments e ON e.student_id = s.id
                LEFT JOIN bagile.orders o ON e.order_id = o.id
                LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
                WHERE o.status = 'completed'
                  AND LOWER(TRIM(COALESCE(o.billing_company, s.company))) IN (
                    SELECT LOWER(TRIM(alias_value)) FROM alias_matches
                )
                  AND e.status NOT IN ('cancelled', 'transferred')
            )
            SELECT
                os.org_name AS Name,
                COALESCE(
                    (SELECT org.primary_domain FROM bagile.organisations org
                     WHERE org.name = os.org_name LIMIT 1),
                    SPLIT_PART(os.domains, ', ', 1)
                ) AS PrimaryDomain,
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
            WITH alias_matches AS (
                SELECT unnest(org.aliases) AS alias_value
                FROM bagile.organisations org
                WHERE org.name ILIKE @organisationName
                   OR LOWER(TRIM(@organisationName)) = ANY(
                       SELECT LOWER(TRIM(a)) FROM UNNEST(org.aliases) a
                   )

                UNION

                SELECT @organisationName AS alias_value
            )
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
            WHERE LOWER(TRIM(COALESCE(o.billing_company, s.company))) IN (
                SELECT LOWER(TRIM(alias_value)) FROM alias_matches
            )
              AND e.status NOT IN ('cancelled', 'transferred')
            GROUP BY COALESCE(cs.sku, 'PRIVATE'), COALESCE(cs.name, 'Private Course')
            ORDER BY TotalCount DESC, LastRunDate DESC NULLS LAST;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<OrganisationCourseHistoryDto>(sql, new { organisationName });
    }
}
