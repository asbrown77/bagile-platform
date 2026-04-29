using Dapper;
using Npgsql;

namespace Bagile.Api.Endpoints;

public static class TransferEndpoints
{
    public static void MapTransferEndpoints(this WebApplication app)
    {
        app.MapPost("/api/enrolments/{id}/mark-refund", MarkRefund);
        app.MapPost("/api/enrolments/{id}/mark-transfer", MarkPendingTransfer);
        app.MapPost("/api/enrolments/{id}/transfer-to/{courseId}", TransferTo);
        // GET /api/transfers/pending already exists in TransfersController
        app.MapPost("/api/course-schedules/{id}/cancel-with-actions", CancelWithActions);
        app.MapPost("/api/admin/enrolments", AdminInsertEnrolment);
        app.MapPost("/api/admin/students", AdminUpsertStudent);
    }

    private static async Task<IResult> MarkRefund(
        long id,
        IConfiguration config)
    {
        var connStr = GetConnStr(config);
        await using var conn = new NpgsqlConnection(connStr);

        var rows = await conn.ExecuteAsync(
            @"UPDATE bagile.enrolments SET status = 'refunded', cancellation_reason = 'provider_cancelled' WHERE id = @id",
            new { id });

        return rows > 0 ? Results.Ok(new { message = "Marked as refunded" }) : Results.NotFound();
    }

    private static async Task<IResult> MarkPendingTransfer(
        long id,
        HttpContext context,
        IConfiguration config)
    {
        var body = await context.Request.ReadFromJsonAsync<MarkTransferRequest>();
        var reason = body?.Reason ?? "provider_cancelled";

        var connStr = GetConnStr(config);
        await using var conn = new NpgsqlConnection(connStr);

        var rows = await conn.ExecuteAsync(
            @"UPDATE bagile.enrolments
              SET status = 'pending_transfer',
                  cancellation_reason = @reason,
                  refund_eligible = @refundEligible
              WHERE id = @id",
            new { id, reason, refundEligible = reason == "provider_cancelled" });

        return rows > 0 ? Results.Ok(new { message = "Marked for transfer" }) : Results.NotFound();
    }

    private static async Task<IResult> TransferTo(
        long id,
        long courseId,
        IConfiguration config)
    {
        var connStr = GetConnStr(config);
        await using var conn = new NpgsqlConnection(connStr);

        // Get existing enrolment
        var old = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, student_id, order_id, course_schedule_id FROM bagile.enrolments WHERE id = @id",
            new { id });

        if (old == null) return Results.NotFound();

        long studentId = (long)old.student_id;
        long? orderId = old.order_id == null ? (long?)null : (long)old.order_id;

        // Check if student already has an active enrolment on the target course
        // (can happen when the enrolment was created manually before this API call)
        var existingId = await conn.ExecuteScalarAsync<long?>(
            @"SELECT id FROM bagile.enrolments
              WHERE student_id = @studentId AND course_schedule_id = @courseId AND status = 'active'
              LIMIT 1",
            new { studentId, courseId });

        long newId;
        if (existingId.HasValue)
        {
            // Already enrolled — treat as already transferred; no duplicate insert
            newId = existingId.Value;
        }
        else
        {
            // Create new enrolment on the target course
            newId = await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, status, transferred_from_enrolment_id, transfer_reason)
                  VALUES (@studentId, @orderId, @courseId, 'active', @oldId, 'CourseTransfer')
                  RETURNING id",
                new { studentId, orderId, courseId, oldId = id });
        }

        // Mark old as transferred
        await conn.ExecuteAsync(
            @"UPDATE bagile.enrolments SET status = 'transferred', transferred_to_enrolment_id = @newId WHERE id = @id",
            new { id, newId });

        return Results.Ok(new { message = $"Transferred to course {courseId}", newEnrolmentId = newId });
    }

    private static async Task<IResult> ListPendingTransfers(IConfiguration config)
    {
        var connStr = GetConnStr(config);
        await using var conn = new NpgsqlConnection(connStr);

        var pending = await conn.QueryAsync<dynamic>(
            @"SELECT e.id AS enrolmentId,
                     s.first_name AS firstName, s.last_name AS lastName, s.email,
                     s.company AS organisation,
                     cs.name AS courseName, cs.sku AS courseCode, cs.start_date AS courseStartDate,
                     e.cancellation_reason AS cancellationReason,
                     e.refund_eligible AS refundEligible
              FROM bagile.enrolments e
              JOIN bagile.students s ON e.student_id = s.id
              JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
              WHERE e.status = 'pending_transfer'
              ORDER BY cs.start_date, s.last_name");

        return Results.Ok(pending);
    }

    private static async Task<IResult> CancelWithActions(
        long id,
        HttpContext context,
        IConfiguration config)
    {
        var body = await context.Request.ReadFromJsonAsync<CancelWithActionsRequest>();
        if (body == null) return Results.BadRequest(new { error = "Request body required" });

        var connStr = GetConnStr(config);
        await using var conn = new NpgsqlConnection(connStr);

        // Cancel the course
        await conn.ExecuteAsync(
            "UPDATE bagile.course_schedules SET status = 'cancelled', last_synced = NOW() WHERE id = @id",
            new { id });

        // Apply actions to each attendee
        foreach (var action in body.AttendeeActions)
        {
            if (action.Action == "refund")
            {
                await conn.ExecuteAsync(
                    @"UPDATE bagile.enrolments SET status = 'refunded', cancellation_reason = 'provider_cancelled'
                      WHERE id = @enrolmentId",
                    new { action.EnrolmentId });
            }
            else if (action.Action == "transfer")
            {
                await conn.ExecuteAsync(
                    @"UPDATE bagile.enrolments SET status = 'pending_transfer',
                        cancellation_reason = 'provider_cancelled', refund_eligible = true
                      WHERE id = @enrolmentId",
                    new { action.EnrolmentId });
            }
        }

        return Results.Ok(new { message = $"Course cancelled, {body.AttendeeActions.Count} attendees updated" });
    }

    private static async Task<IResult> AdminInsertEnrolment(
        AdminInsertEnrolmentRequest body,
        IConfiguration config)
    {
        var connStr = GetConnStr(config);
        await using var conn = new NpgsqlConnection(connStr);

        var id = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id, status, source)
              VALUES (@studentId, @orderId, @courseScheduleId, 'active', 'admin')
              RETURNING id;",
            new { body.StudentId, body.OrderId, body.CourseScheduleId });

        return Results.Ok(new { enrolmentId = id });
    }

    /// <summary>
    /// Find-or-create a student by email. Used for credit-note attendees and other
    /// admin-added cases where no WooCommerce order exists yet, so the ETL would
    /// not create the row. Returns the student ID either way (existing or new),
    /// so callers can chain into POST /api/admin/enrolments without a separate
    /// lookup. Email is the natural key.
    /// </summary>
    private static async Task<IResult> AdminUpsertStudent(
        AdminUpsertStudentRequest body,
        IConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(body.Email))
            return Results.BadRequest(new { error = "Email is required" });

        var connStr = GetConnStr(config);
        await using var conn = new NpgsqlConnection(connStr);

        // Try to find existing student first to keep this idempotent.
        var existingId = await conn.ExecuteScalarAsync<long?>(
            "SELECT id FROM bagile.students WHERE LOWER(email) = LOWER(@email) LIMIT 1;",
            new { body.Email });

        if (existingId.HasValue)
            return Results.Ok(new { studentId = existingId.Value, created = false });

        var newId = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO bagile.students (email, first_name, last_name, company)
              VALUES (@email, @firstName, @lastName, @company)
              RETURNING id;",
            new
            {
                body.Email,
                FirstName = body.FirstName ?? "",
                LastName = body.LastName ?? "",
                Company = body.Company ?? ""
            });

        return Results.Ok(new { studentId = newId, created = true });
    }

    private static string GetConnStr(IConfiguration config) =>
        config.GetConnectionString("DefaultConnection")
        ?? config.GetValue<string>("ConnectionStrings:DefaultConnection")
        ?? throw new InvalidOperationException("Connection string not found");

    private record MarkTransferRequest(string? Reason);
    private record AttendeeAction(long EnrolmentId, string Action); // "refund" or "transfer"
    private record AdminInsertEnrolmentRequest(long StudentId, long? OrderId, long CourseScheduleId);
    private record AdminUpsertStudentRequest(string Email, string? FirstName, string? LastName, string? Company);
    private record CancelWithActionsRequest(List<AttendeeAction> AttendeeActions);
}
