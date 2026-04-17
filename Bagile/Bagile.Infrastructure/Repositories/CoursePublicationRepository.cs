using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class CoursePublicationRepository : ICoursePublicationRepository
{
    private readonly string _connStr;

    public CoursePublicationRepository(string connStr)
    {
        _connStr = connStr;
    }

    public async Task<CoursePublication?> GetByPlannedCourseAndGatewayAsync(
        int plannedCourseId, string gateway, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id,
                   planned_course_id AS PlannedCourseId,
                   course_schedule_id AS CourseScheduleId,
                   gateway AS Gateway,
                   published_at AS PublishedAt,
                   external_url AS ExternalUrl,
                   woocommerce_product_id AS WoocommerceProductId,
                   created_at AS CreatedAt
            FROM bagile.course_publications
            WHERE planned_course_id = @plannedCourseId
              AND gateway = @gateway;";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryFirstOrDefaultAsync<CoursePublication>(sql, new { plannedCourseId, gateway });
    }

    public async Task<CoursePublication?> GetByScheduleAndGatewayAsync(
        long courseScheduleId, string gateway, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id AS Id,
                   planned_course_id AS PlannedCourseId,
                   course_schedule_id AS CourseScheduleId,
                   gateway AS Gateway,
                   published_at AS PublishedAt,
                   external_url AS ExternalUrl,
                   woocommerce_product_id AS WoocommerceProductId,
                   created_at AS CreatedAt
            FROM bagile.course_publications
            WHERE course_schedule_id = @courseScheduleId
              AND gateway = @gateway;";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryFirstOrDefaultAsync<CoursePublication>(sql, new { courseScheduleId, gateway });
    }

    public async Task<int> InsertAsync(CoursePublication publication, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO bagile.course_publications
                (planned_course_id, course_schedule_id, gateway, published_at,
                 external_url, woocommerce_product_id)
            VALUES
                (@PlannedCourseId, @CourseScheduleId, @Gateway, @PublishedAt,
                 @ExternalUrl, @WoocommerceProductId)
            RETURNING id;";

        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.ExecuteScalarAsync<int>(sql, publication);
    }
}
