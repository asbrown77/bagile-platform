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