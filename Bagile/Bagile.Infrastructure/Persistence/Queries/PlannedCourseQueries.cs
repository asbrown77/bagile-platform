using Bagile.Application.Common.Interfaces;
using Bagile.Application.PlannedCourses.DTOs;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Persistence.Queries;

public class PlannedCourseQueries : IPlannedCourseQueries
{
    private readonly string _connectionString;

    public PlannedCourseQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    // Intermediate type used only for hydrating publication rows (includes the FK).
    private record PublicationRow
    {
        public int Id { get; init; }
        public int PlannedCourseId { get; init; }
        public string Gateway { get; init; } = "";
        public DateTime? PublishedAt { get; init; }
        public string? ExternalUrl { get; init; }
        public int? WoocommerceProductId { get; init; }
    }

    public async Task<PlannedCourseDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        const string courseSql = @"
            SELECT pc.id                AS Id,
                   pc.course_type       AS CourseType,
                   pc.trainer_id        AS TrainerId,
                   t.name               AS TrainerName,
                   pc.start_date        AS StartDate,
                   pc.end_date          AS EndDate,
                   pc.is_virtual        AS IsVirtual,
                   pc.venue             AS Venue,
                   pc.notes             AS Notes,
                   pc.decision_deadline AS DecisionDeadline,
                   pc.is_private        AS IsPrivate,
                   pc.status            AS Status,
                   pc.created_at        AS CreatedAt,
                   pc.updated_at        AS UpdatedAt
            FROM bagile.planned_courses pc
            JOIN bagile.trainers t ON t.id = pc.trainer_id
            WHERE pc.id = @id;";

        const string pubSql = @"
            SELECT cp.id                      AS Id,
                   cp.planned_course_id       AS PlannedCourseId,
                   cp.gateway                 AS Gateway,
                   cp.published_at            AS PublishedAt,
                   cp.external_url            AS ExternalUrl,
                   cp.woocommerce_product_id  AS WoocommerceProductId
            FROM bagile.course_publications cp
            WHERE cp.planned_course_id = @id
            ORDER BY cp.created_at;";

        await using var conn = new NpgsqlConnection(_connectionString);

        var course = await conn.QueryFirstOrDefaultAsync<PlannedCourseDto>(
            new CommandDefinition(courseSql, new { id }, cancellationToken: ct));

        if (course is null) return null;

        var publications = await conn.QueryAsync<PublicationRow>(
            new CommandDefinition(pubSql, new { id }, cancellationToken: ct));

        return course with
        {
            Publications = publications
                .Select(p => new CoursePublicationDto
                {
                    Id = p.Id,
                    Gateway = p.Gateway,
                    PublishedAt = p.PublishedAt,
                    ExternalUrl = p.ExternalUrl,
                    WoocommerceProductId = p.WoocommerceProductId,
                })
                .ToList()
        };
    }

    public async Task<IEnumerable<PlannedCourseDto>> GetAllAsync(CancellationToken ct = default)
    {
        // LEFT JOIN so an orphaned trainer_id doesn't drop the row from the list.
        const string courseSql = @"
            SELECT pc.id                AS Id,
                   pc.course_type       AS CourseType,
                   pc.trainer_id        AS TrainerId,
                   t.name               AS TrainerName,
                   pc.start_date        AS StartDate,
                   pc.end_date          AS EndDate,
                   pc.is_virtual        AS IsVirtual,
                   pc.venue             AS Venue,
                   pc.notes             AS Notes,
                   pc.decision_deadline AS DecisionDeadline,
                   pc.is_private        AS IsPrivate,
                   pc.status            AS Status,
                   pc.created_at        AS CreatedAt,
                   pc.updated_at        AS UpdatedAt
            FROM bagile.planned_courses pc
            LEFT JOIN bagile.trainers t ON t.id = pc.trainer_id
            ORDER BY pc.start_date;";

        const string pubSql = @"
            SELECT cp.id                      AS Id,
                   cp.planned_course_id       AS PlannedCourseId,
                   cp.gateway                 AS Gateway,
                   cp.published_at            AS PublishedAt,
                   cp.external_url            AS ExternalUrl,
                   cp.woocommerce_product_id  AS WoocommerceProductId
            FROM bagile.course_publications cp
            WHERE cp.planned_course_id IS NOT NULL
            ORDER BY cp.planned_course_id, cp.created_at;";

        await using var conn = new NpgsqlConnection(_connectionString);

        var courses = (await conn.QueryAsync<PlannedCourseDto>(
            new CommandDefinition(courseSql, cancellationToken: ct))).ToList();

        if (courses.Count == 0) return courses;

        var publications = await conn.QueryAsync<PublicationRow>(
            new CommandDefinition(pubSql, cancellationToken: ct));

        var pubsByPlannedCourse = publications
            .GroupBy(p => p.PlannedCourseId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CoursePublicationDto>)g.Select(p => new CoursePublicationDto
                {
                    Id = p.Id,
                    Gateway = p.Gateway,
                    PublishedAt = p.PublishedAt,
                    ExternalUrl = p.ExternalUrl,
                    WoocommerceProductId = p.WoocommerceProductId,
                }).ToList());

        return courses.Select(c =>
            pubsByPlannedCourse.TryGetValue(c.Id, out var pubs)
                ? c with { Publications = pubs }
                : c);
    }
}
