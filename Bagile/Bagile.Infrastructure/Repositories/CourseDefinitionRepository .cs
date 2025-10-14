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
            "SELECT * FROM course_definitions ORDER BY code;");
    }

    public async Task<CourseDefinition?> GetByCodeAsync(string code)
    {
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QueryFirstOrDefaultAsync<CourseDefinition>(
            "SELECT * FROM course_definitions WHERE code = @code;", new { code });
    }
}