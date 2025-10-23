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

    public async Task UpsertAsync(CourseSchedule course)
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
        await conn.ExecuteAsync(sql, course);
    }

    public async Task<IEnumerable<CourseSchedule>> GetAllAsync()
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryAsync<CourseSchedule>(
            "SELECT * FROM bagile.course_schedules ORDER BY start_date DESC;");
    }
}