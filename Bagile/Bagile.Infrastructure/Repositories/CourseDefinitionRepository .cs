using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class CourseDefinitionRepository : ICourseDefinitionRepository
{
    private readonly string _connStr;

    public CourseDefinitionRepository(string connStr)
    {
        _connStr = connStr;
    }

    public async Task<IEnumerable<CourseDefinition>> GetAllAsync()
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryAsync<CourseDefinition>(
            "SELECT id, code, name, description, duration_days, active, badge_url FROM bagile.course_definitions ORDER BY code;");
    }

    public async Task<CourseDefinition?> GetByCodeAsync(string code)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryFirstOrDefaultAsync<CourseDefinition>(
            "SELECT id, code, name, description, duration_days, active, badge_url FROM bagile.course_definitions WHERE code = @code;",
            new { code });
    }

    public async Task UpdateBadgeUrlAsync(string code, string? badgeUrl)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(
            "UPDATE bagile.course_definitions SET badge_url = @badgeUrl WHERE code = @code;",
            new { code, badgeUrl });
    }
}