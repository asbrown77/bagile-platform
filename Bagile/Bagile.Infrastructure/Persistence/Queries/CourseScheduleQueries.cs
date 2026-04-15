using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using Bagile.Application.CourseSchedules.Queries.GetScheduleConflicts;
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
                cs.trainer_name AS TrainerName,
                cs.format_type AS FormatType,
                CASE WHEN cs.is_public THEN 'public' ELSE 'private' END AS Type,
                cs.status AS Status,
                cs.capacity AS Capacity,
                COUNT(e.id) AS CurrentEnrolmentCount,
                CASE WHEN COUNT(e.id) >= 3 THEN true ELSE false END AS GuaranteedToRun,
                CASE
                    WHEN cs.start_date <= CURRENT_DATE + INTERVAL '7 days'
                         AND COUNT(e.id) < 3
                    THEN true
                    ELSE false
                END AS NeedsAttention,
                org.name AS ClientOrganisationName
            FROM bagile.course_schedules cs
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id AND e.status NOT IN ('cancelled', 'transferred')
            LEFT JOIN bagile.organisations org ON org.id = cs.client_organisation_id
            WHERE 1=1
            " + (from != null ? " AND cs.start_date >= @from" : "") + @"
            " + (to != null ? " AND cs.start_date <= @to" : "") + @"
            " + (courseCode != null ? " AND cs.sku ILIKE @courseCodePattern" : "") + @"
            " + (trainer != null ? " AND cs.trainer_name ILIKE @trainerPattern" : "") + @"
            " + (type != null ? " AND ((cs.is_public = true AND @type = 'public') OR (cs.is_public = false AND @type = 'private'))" : "") + @"
            " + (status != null ? " AND cs.status = @status" : "") + @"
            GROUP BY cs.id, cs.sku, cs.name, cs.start_date, cs.end_date,
                     cs.format_type, cs.trainer_name, cs.is_public, cs.status, cs.capacity,
                     cs.client_organisation_id, org.name
            ORDER BY cs.start_date DESC
            LIMIT @pageSize OFFSET @offset;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CourseScheduleDto>(sql, new
        {
            from,
            to,
            courseCodePattern = courseCode != null ? $"{courseCode}-%" : null,
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
            courseCodePattern = courseCode != null ? $"{courseCode}-%" : null,
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
                cs.client_organisation_id AS ClientOrganisationId,
                org.name AS ClientOrganisationName,
                org.acronym AS ClientOrganisationAcronym,
                org.contact_email AS ClientOrganisationContactEmail,
                cs.invoice_reference AS InvoiceReference,
                cs.meeting_url AS MeetingUrl,
                cs.meeting_id AS MeetingId,
                cs.meeting_passcode AS MeetingPasscode,
                cs.venue_address AS VenueAddress,
                cs.notes AS Notes,
                COUNT(e.id) AS CurrentEnrolmentCount,
                CASE WHEN COUNT(e.id) >= 3 THEN true ELSE false END AS GuaranteedToRun,
                CASE
                    WHEN cs.start_date <= CURRENT_DATE + INTERVAL '7 days'
                         AND COUNT(e.id) < 3
                    THEN true
                    ELSE false
                END AS NeedsAttention
            FROM bagile.course_schedules cs
            LEFT JOIN bagile.organisations org ON org.id = cs.client_organisation_id
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id AND e.status NOT IN ('cancelled', 'transferred')
            WHERE cs.id = @scheduleId
            GROUP BY cs.id, cs.sku, cs.name, cs.start_date, cs.end_date, cs.format_type,
                     cs.is_public, cs.status, cs.capacity, cs.price, cs.trainer_name,
                     cs.source_system, cs.source_product_id, cs.last_synced,
                     cs.client_organisation_id, org.name, org.acronym, org.contact_email,
                     cs.invoice_reference, cs.meeting_url, cs.meeting_id,
                     cs.meeting_passcode, cs.venue_address, cs.notes;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<CourseScheduleDetailDto>(sql, new { scheduleId });
    }

    public async Task<IEnumerable<CourseMonitoringRawDto>> GetCourseMonitoringDataAsync(
        int daysAhead,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT
                cs.id AS Id,
                COALESCE(cs.sku, '') AS CourseCode,
                cs.name AS Title,
                cs.start_date AS StartDate,
                cs.end_date AS EndDate,
                cs.trainer_name AS TrainerName,
                cs.format_type AS Location,
                cs.status AS Status,
                COUNT(e.id) AS CurrentEnrolmentCount
            FROM bagile.course_schedules cs
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id AND e.status NOT IN ('cancelled', 'transferred')
            WHERE cs.start_date >= CURRENT_DATE - INTERVAL '2 days'
              AND cs.start_date <= CURRENT_DATE + @daysAhead * INTERVAL '1 day'
              AND cs.is_public = true
            GROUP BY cs.id, cs.sku, cs.name, cs.start_date, cs.end_date,
                     cs.trainer_name, cs.format_type, cs.status
            ORDER BY cs.start_date ASC;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CourseMonitoringRawDto>(sql, new { daysAhead });
    }

    public async Task<IEnumerable<CourseAttendeeDto>> GetCourseAttendeesAsync(
        long scheduleId,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT
                e.id AS EnrolmentId,
                s.id AS StudentId,
                s.first_name AS FirstName,
                s.last_name AS LastName,
                CONCAT(s.first_name, ' ', s.last_name) AS Name,
                s.email AS Email,
                s.company AS Organisation,
                e.status AS Status,
                cs.sku AS CourseCode,
                cs.name AS CourseName,
                s.country AS Country,
                o.external_id AS OrderNumber,
                o.total_amount AS OrderAmount,
                o.status AS OrderStatus,
                o.currency AS Currency,
                o.billing_company AS BillingCompany,
                o.contact_name AS BillingName,
                o.contact_email AS BillingEmail,
                COALESCE(o.payment_method_title, o.payment_method) AS PaymentMethod,
                o.total_quantity AS OrderAttendeeCount
            FROM bagile.enrolments e
            JOIN bagile.students s ON e.student_id = s.id
            JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
            LEFT JOIN bagile.orders o ON e.order_id = o.id
            WHERE e.course_schedule_id = @scheduleId
              AND e.status != 'cancelled'
            ORDER BY s.last_name, s.first_name;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<CourseAttendeeDto>(sql, new { scheduleId });
    }

    public async Task<IEnumerable<ScheduleConflictDto>> GetScheduleConflictsAsync(
        DateTime startDate,
        DateTime endDate,
        string? trainerName,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT
                cs.id AS ConflictingCourseId,
                cs.name AS CourseName,
                COALESCE(cs.sku, '') AS CourseCode,
                cs.start_date AS StartDate,
                cs.end_date AS EndDate,
                CASE WHEN cs.is_public THEN 'public' ELSE 'private' END AS Type,
                cs.trainer_name AS TrainerName,
                COUNT(e.id)::int AS EnrolmentCount,
                CASE WHEN COUNT(e.id) >= 3 THEN true ELSE false END
                    AS IsGuaranteedToRun,
                CASE
                    WHEN cs.trainer_name IS NOT NULL
                         AND cs.trainer_name ILIKE @trainerPattern
                    THEN 'trainer_clash'
                    ELSE 'date_overlap'
                END AS ConflictType
            FROM bagile.course_schedules cs
            LEFT JOIN bagile.enrolments e ON e.course_schedule_id = cs.id
                AND e.status NOT IN ('cancelled', 'transferred')
            WHERE cs.status NOT IN ('cancelled', 'completed')
              AND cs.start_date <= @endDate
              AND COALESCE(cs.end_date, cs.start_date) >= @startDate
            GROUP BY cs.id
            ORDER BY cs.start_date;";

        var trainerPattern = string.IsNullOrWhiteSpace(trainerName)
            ? "%" : $"%{trainerName}%";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<ScheduleConflictDto>(sql,
            new { startDate, endDate, trainerPattern });
    }
}