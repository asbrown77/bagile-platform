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

    private const string SelectColumns =
        "id, code, name, description, duration_days AS DurationDays, active, badge_url AS BadgeUrl, provider AS Provider";

    public async Task<IEnumerable<CourseDefinition>> GetAllAsync()
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryAsync<CourseDefinition>(
            $"SELECT {SelectColumns} FROM bagile.course_definitions ORDER BY code;");
    }

    public async Task<CourseDefinition?> GetByCodeAsync(string code)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryFirstOrDefaultAsync<CourseDefinition>(
            $"SELECT {SelectColumns} FROM bagile.course_definitions WHERE code = @code;",
            new { code });
    }

    public async Task UpdateBadgeUrlAsync(string code, string? badgeUrl)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(
            "UPDATE bagile.course_definitions SET badge_url = @badgeUrl WHERE code = @code;",
            new { code, badgeUrl });
    }

    public async Task UpdateDurationAsync(string code, int durationDays)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(
            "UPDATE bagile.course_definitions SET duration_days = @durationDays WHERE code = @code;",
            new { code, durationDays });
    }

    public async Task UpdateNameAsync(string code, string name)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(
            "UPDATE bagile.course_definitions SET name = @name WHERE code = @code;",
            new { code, name });
    }

    public async Task UpdateProviderAsync(string code, string? provider)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(
            "UPDATE bagile.course_definitions SET provider = @provider WHERE code = @code;",
            new { code, provider });
    }

    /// <summary>
    /// Returns a dictionary of normalised code → provider for all active definitions.
    /// Keys are uppercased with hyphens/underscores stripped to match SKU-extracted types.
    /// </summary>
    public async Task<Dictionary<string, string?>> GetProviderMapAsync()
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var rows = await conn.QueryAsync<(string Code, string? Provider)>(
            "SELECT code, provider FROM bagile.course_definitions WHERE active = true;");
        return rows.ToDictionary(
            r => r.Code.ToUpperInvariant().Replace("-", "").Replace("_", ""),
            r => r.Provider,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<CourseDefinition> CreateAsync(string code, string name, int durationDays)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleAsync<CourseDefinition>(
            $@"INSERT INTO bagile.course_definitions (code, name, duration_days, active)
               VALUES (@code, @name, @durationDays, true)
               RETURNING {SelectColumns};",
            new { code, name, durationDays });
    }

    public async Task<IEnumerable<string>> GetAliasesAsync(string code)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryAsync<string>(
            "SELECT alias FROM bagile.course_code_aliases WHERE canonical_code = @code ORDER BY alias;",
            new { code });
    }

    public async Task AddAliasAsync(string code, string alias)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(
            "INSERT INTO bagile.course_code_aliases (alias, canonical_code) VALUES (@alias, @code);",
            new { code, alias });
    }

    public async Task<bool> RemoveAliasAsync(string code, string alias)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        var rows = await conn.ExecuteAsync(
            "DELETE FROM bagile.course_code_aliases WHERE alias = @alias AND canonical_code = @code;",
            new { code, alias });
        return rows > 0;
    }

    public async Task<bool> AliasExistsAsync(string alias)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM bagile.course_code_aliases WHERE alias = @alias);",
            new { alias });
    }
}