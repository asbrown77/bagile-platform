using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class CourseScheduleRepository : ICourseScheduleRepository
{
    private readonly string _connStr;

    public CourseScheduleRepository(string connStr)
    {
        _connStr = connStr;
    }

    public async Task UpsertAsync(CourseSchedule schedule)
    {
        const string sql = @"
            INSERT INTO bagile.course_schedules
                (name, status, start_date, end_date, capacity, price, sku,
                 trainer_name, format_type, is_public,
                 source_system, source_product_id, last_synced)
            VALUES
                (@Name, @Status, @StartDate, @EndDate, @Capacity, @Price, @Sku,
                 @TrainerName, @FormatType, @IsPublic,
                 @SourceSystem, @SourceProductId, now())
            ON CONFLICT (source_system, source_product_id)
            DO UPDATE SET
                name = EXCLUDED.name,
                status = EXCLUDED.status,
                start_date = EXCLUDED.start_date,
                end_date = EXCLUDED.end_date,
                capacity = EXCLUDED.capacity,
                price = EXCLUDED.price,
                sku = EXCLUDED.sku,
                trainer_name = EXCLUDED.trainer_name,
                format_type = EXCLUDED.format_type,
                is_public = EXCLUDED.is_public,
                last_synced = now();";

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(sql, schedule);
    }

    public async Task<long> UpsertFromWooPayloadAsync(
        long productId,
        string? courseName,
        string? sku,
        DateTime? startDate,
        DateTime? endDate,
        decimal? price,
        string? currency // ignored since table has no currency column
    )
    {
        const string sql = @"
        INSERT INTO bagile.course_schedules (
            source_system,
            source_product_id,
            name,
            sku,
            start_date,
            end_date,
            price,
            last_synced
        )
        VALUES (
            'woo',
            @productId,
            @name,
            @sku,
            @startDate,
            @endDate,
            @price,
            now()
        )
        ON CONFLICT (source_system, source_product_id)
        DO UPDATE SET
            name = EXCLUDED.name,
            sku = EXCLUDED.sku,
            start_date = EXCLUDED.start_date,
            end_date = EXCLUDED.end_date,
            price = EXCLUDED.price,
            last_synced = now()
        RETURNING id;";

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("productId", productId);
        cmd.Parameters.AddWithValue("name", (object?)courseName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sku", (object?)sku ?? DBNull.Value);
        cmd.Parameters.AddWithValue("startDate", (object?)startDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("endDate", (object?)endDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("price", (object?)price ?? DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }


    public async Task<long?> GetIdBySourceProductAsync(long sourceProductId)
    {
        const string sql = @"
                SELECT id
                FROM bagile.course_schedules
                WHERE source_product_id = @sourceProductId
                LIMIT 1;";

        await using var c = new NpgsqlConnection(_connStr);
        return await c.QueryFirstOrDefaultAsync<long?>(sql, new { sourceProductId });
    }

    public async Task<CourseSchedule?> GetBySourceProductIdAsync(long sourceProductId)
    {
        const string sql = @"
                SELECT *
                FROM bagile.course_schedules
                WHERE source_product_id = @sourceProductId";

        await using var c = new NpgsqlConnection(_connStr);
        return await c.QueryFirstOrDefaultAsync<CourseSchedule?>(sql, new { sourceProductId });
    }
    

    public async Task<IEnumerable<CourseSchedule>> GetAllAsync()
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryAsync<CourseSchedule>(
            "SELECT * FROM bagile.course_schedules ORDER BY start_date DESC;");
    }

    public async Task UpdateStatusAsync(long scheduleId, string status)
    {
        const string sql = @"
            UPDATE bagile.course_schedules
            SET status = @status, last_synced = now()
            WHERE id = @scheduleId;";

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(sql, new { scheduleId, status });
    }

    public async Task<long> InsertPrivateCourseAsync(CourseSchedule schedule)
    {
        const string sql = @"
            INSERT INTO bagile.course_schedules
                (name, status, start_date, end_date, capacity, price, sku,
                 trainer_name, format_type, is_public,
                 source_system, client_organisation_id, notes, created_by,
                 invoice_reference, meeting_url, meeting_id, meeting_passcode,
                 venue_address, last_synced)
            VALUES
                (@Name, @Status, @StartDate, @EndDate, @Capacity, @Price, @Sku,
                 @TrainerName, @FormatType, @IsPublic,
                 @SourceSystem, @ClientOrganisationId, @Notes, @CreatedBy,
                 @InvoiceReference, @MeetingUrl, @MeetingId, @MeetingPasscode,
                 @VenueAddress, now())
            RETURNING id;";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.ExecuteScalarAsync<long>(sql, schedule);
    }

    public async Task<bool> ExistsBySkuAsync(string sku)
    {
        const string sql = "SELECT EXISTS(SELECT 1 FROM bagile.course_schedules WHERE sku = @sku);";
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.ExecuteScalarAsync<bool>(sql, new { sku });
    }

    public async Task UpdatePrivateCourseAsync(long id, UpdatePrivateCourseFields f)
    {
        const string sql = @"
            UPDATE bagile.course_schedules
            SET name              = @Name,
                trainer_name      = @TrainerName,
                start_date        = @StartDate,
                end_date          = @EndDate,
                capacity          = @Capacity,
                price             = @Price,
                invoice_reference = @InvoiceReference,
                venue_address     = @VenueAddress,
                meeting_url       = @MeetingUrl,
                meeting_id        = @MeetingId,
                meeting_passcode  = @MeetingPasscode,
                notes             = @Notes,
                last_synced       = now()
            WHERE id = @Id
              AND is_public = false;";

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(sql, new
        {
            Id = id,
            f.Name,
            f.TrainerName,
            f.StartDate,
            f.EndDate,
            f.Capacity,
            f.Price,
            f.InvoiceReference,
            f.VenueAddress,
            f.MeetingUrl,
            f.MeetingId,
            f.MeetingPasscode,
            f.Notes,
        });
    }

    public async Task<long?> GetIdBySkuAsync(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
            return null;

        const string sql = @"
        SELECT id
        FROM bagile.course_schedules
        WHERE sku = @Sku
        LIMIT 1;";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryFirstOrDefaultAsync<long?>(sql, new { Sku = sku });
    }
}