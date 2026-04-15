using Bagile.Application.Calendar.DTOs;
using Bagile.Application.Common.Interfaces;
using Bagile.Domain.Calendar;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class CalendarQueries : ICalendarQueries
{
    private readonly string _connectionString;
    private readonly string _wooSiteUrl;

    public CalendarQueries(string connectionString, string wooSiteUrl = "")
    {
        _connectionString = connectionString;
        _wooSiteUrl = wooSiteUrl.TrimEnd('/');
    }

    public async Task<IEnumerable<CalendarEventDto>> GetCalendarEventsAsync(
        DateTime from,
        DateTime to,
        int? trainerId,
        CancellationToken ct = default)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var planned = await GetPlannedCoursesAsync(conn, from, to, trainerId);
        var live = await GetLiveCoursesAsync(conn, from, to, trainerId, _wooSiteUrl);

        return planned.Concat(live).OrderBy(e => e.StartDate).ThenBy(e => e.CourseType);
    }

    private static async Task<IEnumerable<CalendarEventDto>> GetPlannedCoursesAsync(
        NpgsqlConnection conn,
        DateTime from,
        DateTime to,
        int? trainerId)
    {
        // Fetch planned courses with trainer info and publication status
        var sql = @"
            SELECT pc.id,
                   pc.course_type,
                   pc.start_date,
                   pc.end_date,
                   pc.is_virtual,
                   pc.is_private,
                   pc.status,
                   pc.decision_deadline,
                   pc.venue,
                   pc.notes,
                   t.id   AS trainer_id,
                   t.name AS trainer_name
            FROM bagile.planned_courses pc
            JOIN bagile.trainers t ON t.id = pc.trainer_id
            WHERE pc.start_date >= @from
              AND pc.start_date <= @to"
            + (trainerId.HasValue ? " AND pc.trainer_id = @trainerId" : "") + @"
            ORDER BY pc.start_date;";

        var courses = (await conn.QueryAsync<PlannedCourseRow>(sql,
            new { from, to, trainerId })).ToList();

        if (courses.Count == 0)
            return Enumerable.Empty<CalendarEventDto>();

        // Fetch all publications for these planned courses in one query
        var ids = courses.Select(c => c.Id).ToArray();
        var pubs = (await conn.QueryAsync<PublicationRow>(
            @"SELECT planned_course_id AS CourseId, gateway, published_at, external_url
              FROM bagile.course_publications
              WHERE planned_course_id = ANY(@ids)",
            new { ids })).ToLookup(p => p.CourseId);

        return courses.Select(c =>
        {
            var applicableGateways = GatewayConfig.GetGatewaysForCourseType(c.CourseType);
            var coursePubs = pubs[c.Id].ToList();

            var gateways = applicableGateways.Select(g =>
            {
                var pub = coursePubs.FirstOrDefault(p =>
                    string.Equals(p.Gateway, g, StringComparison.OrdinalIgnoreCase));
                return new GatewayStatusDto
                {
                    Type = g,
                    Published = pub?.PublishedAt != null,
                    Url = pub?.ExternalUrl
                };
            }).ToList();

            var status = DeriveStatus(c.Status, gateways);

            return new CalendarEventDto
            {
                Id = $"planned-{c.Id}",
                CourseType = c.CourseType,
                TrainerInitials = GetInitials(c.TrainerName),
                TrainerName = c.TrainerName,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsVirtual = c.IsVirtual,
                IsPrivate = c.IsPrivate,
                Status = status,
                DecisionDeadline = c.DecisionDeadline ?? SafeAddDays(c.StartDate, -10),
                EnrolmentCount = 0, // planned courses have no enrolments yet
                MinimumEnrolments = 3,
                Venue = c.Venue,
                Notes = c.Notes,
                Gateways = gateways
            };
        });
    }

    private static async Task<IEnumerable<CalendarEventDto>> GetLiveCoursesAsync(
        NpgsqlConnection conn,
        DateTime from,
        DateTime to,
        int? trainerId,
        string wooSiteUrl)
    {
        // For live courses, trainer is stored as free text (trainer_name).
        // We need to join to trainers table if trainerId filter is specified.
        var sql = @"
            SELECT cs.id                          AS Id,
                   COALESCE(cs.sku, '')           AS Sku,
                   cs.start_date                  AS StartDate,
                   cs.end_date                    AS EndDate,
                   cs.format_type                 AS FormatType,
                   cs.is_public                   AS IsPublic,
                   cs.status                      AS Status,
                   cs.trainer_name                AS TrainerName,
                   cs.venue_address               AS Venue,
                   cs.notes                       AS Notes,
                   cs.source_product_id           AS SourceProductId,
                   COUNT(e.id)::int               AS EnrolmentCount
            FROM bagile.course_schedules cs
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id
                AND e.status NOT IN ('cancelled', 'transferred')
            WHERE cs.start_date IS NOT NULL
              AND cs.start_date >= @from
              AND cs.start_date <= @to"
            + (trainerId.HasValue
                ? @" AND cs.trainer_name = (SELECT name FROM bagile.trainers WHERE id = @trainerId)"
                : "") + @"
            GROUP BY cs.id
            ORDER BY cs.start_date;";

        var courses = (await conn.QueryAsync<LiveCourseRow>(sql,
            new { from, to, trainerId })).ToList();

        if (courses.Count == 0)
            return Enumerable.Empty<CalendarEventDto>();

        // Fetch publications for these course schedules
        var ids = courses.Select(c => c.Id).ToArray();
        var pubs = (await conn.QueryAsync<PublicationRow>(
            @"SELECT course_schedule_id AS CourseId, gateway, published_at, external_url
              FROM bagile.course_publications
              WHERE course_schedule_id = ANY(@ids)",
            new { ids })).ToLookup(p => p.CourseId);

        return courses.Select(c =>
        {
            var courseType = ExtractCourseType(c.Sku);
            var applicableGateways = GatewayConfig.GetGatewaysForCourseType(courseType);
            var coursePubs = pubs[c.Id].ToList();

            // Legacy WooCommerce courses pre-date the publication tracking system.
            // All applicable gateways are treated as published — they were live before
            // this portal existed. Use any stored URL if present; for e-commerce fall
            // back to the WooCommerce shortlink (?p=ID) derived from source_product_id.
            var gateways = applicableGateways.Select(g =>
            {
                var pub = coursePubs.FirstOrDefault(p =>
                    string.Equals(p.Gateway, g, StringComparison.OrdinalIgnoreCase));
                var url = pub?.ExternalUrl ?? DeriveUrl(g, c.SourceProductId, wooSiteUrl);
                return new GatewayStatusDto
                {
                    Type = g,
                    Published = true,
                    Url = url
                };
            }).ToList();

            var status = c.Status == "cancelled"
                ? "cancelled"
                : DeriveStatus("planned", gateways);

            return new CalendarEventDto
            {
                Id = $"schedule-{c.Id}",
                CourseType = courseType,
                TrainerInitials = GetInitials(c.TrainerName),
                TrainerName = c.TrainerName,
                StartDate = c.StartDate,
                EndDate = c.EndDate ?? c.StartDate,
                IsVirtual = string.Equals(c.FormatType, "virtual", StringComparison.OrdinalIgnoreCase),
                IsPrivate = !c.IsPublic,
                Status = status,
                DecisionDeadline = SafeAddDays(c.StartDate, -10), // default for legacy courses
                EnrolmentCount = c.EnrolmentCount,
                MinimumEnrolments = 3,
                Venue = c.Venue,
                Notes = c.Notes,
                Gateways = gateways
            };
        });
    }

    /// <summary>
    /// Derive a gateway URL when no explicit publication record exists.
    /// For e-commerce: construct WooCommerce shortlink from the product ID.
    /// </summary>
    private static string? DeriveUrl(string gateway, long? sourceProductId, string wooSiteUrl)
    {
        if (gateway == "ecommerce" && sourceProductId.HasValue && sourceProductId > 0
            && !string.IsNullOrEmpty(wooSiteUrl))
        {
            return $"{wooSiteUrl}/?p={sourceProductId}";
        }
        return null;
    }

    /// <summary>
    /// Derive calendar status from base status and gateway publication state.
    /// </summary>
    private static string DeriveStatus(string baseStatus, List<GatewayStatusDto> gateways)
    {
        if (baseStatus == "cancelled")
            return "cancelled";

        var publishedCount = gateways.Count(g => g.Published);

        if (publishedCount == 0)
            return "planned";
        if (publishedCount == gateways.Count)
            return "live";
        return "partial_live";
    }

    /// <summary>
    /// Extract course type prefix from SKU. E.g. "PSPO-300326-AB" => "PSPO",
    /// "PSMAI-280526-CB" => "PSMAI", "PSM-PRIV-150426" => "PSM"
    /// </summary>
    private static string ExtractCourseType(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return "";

        // SKU format: COURSETYPE-DDMMYY-INITIALS or COURSETYPE-PRIV-DDMMYY
        var parts = sku.Split('-');
        return parts[0];
    }

    /// <summary>
    /// Returns date.AddDays(days) clamped to DateTime.MinValue/MaxValue to avoid overflow.
    /// Needed because some legacy course_schedules rows have edge-case dates.
    /// </summary>
    private static DateTime SafeAddDays(DateTime date, int days)
    {
        if (days < 0 && date < DateTime.MinValue.AddDays(-days))
            return DateTime.MinValue;
        if (days > 0 && date > DateTime.MaxValue.AddDays(-days))
            return DateTime.MaxValue;
        return date.AddDays(days);
    }

    /// <summary>
    /// Get trainer initials from full name. "Alex Brown" => "AB", "Chris Bexon" => "CB"
    /// </summary>
    private static string? GetInitials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p => char.ToUpperInvariant(p[0])));
    }

    // ── Internal row types for Dapper mapping ──────────────────

    private class PlannedCourseRow
    {
        public int Id { get; set; }
        public string CourseType { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsVirtual { get; set; }
        public bool IsPrivate { get; set; }
        public string Status { get; set; } = "";
        public DateTime? DecisionDeadline { get; set; }
        public string? Venue { get; set; }
        public string? Notes { get; set; }
        public int TrainerId { get; set; }
        public string TrainerName { get; set; } = "";
    }

    private class LiveCourseRow
    {
        public long Id { get; set; }
        public string Sku { get; set; } = "";
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? FormatType { get; set; }
        public bool IsPublic { get; set; }
        public string? Status { get; set; }
        public string? TrainerName { get; set; }
        public string? Venue { get; set; }
        public string? Notes { get; set; }
        public long? SourceProductId { get; set; }
        public int EnrolmentCount { get; set; }
    }

    private class PublicationRow
    {
        public long CourseId { get; set; }
        public string Gateway { get; set; } = "";
        public DateTime? PublishedAt { get; set; }
        public string? ExternalUrl { get; set; }
    }
}
