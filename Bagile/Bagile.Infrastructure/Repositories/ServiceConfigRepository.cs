using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class ServiceConfigRepository : IServiceConfigRepository
{
    private readonly string _connStr;

    public ServiceConfigRepository(string connStr)
    {
        _connStr = connStr;
    }

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        const string sql = "SELECT value FROM bagile.service_config WHERE key = @key";
        await using var conn = new NpgsqlConnection(_connStr);
        return await conn.QuerySingleOrDefaultAsync<string?>(sql, new { key });
    }

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO bagile.service_config (key, value, updated_at)
            VALUES (@key, @value, now())
            ON CONFLICT (key) DO UPDATE
                SET value      = EXCLUDED.value,
                    updated_at = now()";

        await using var conn = new NpgsqlConnection(_connStr);
        await conn.ExecuteAsync(sql, new { key, value });
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = "SELECT key, value FROM bagile.service_config ORDER BY key";
        await using var conn = new NpgsqlConnection(_connStr);
        var rows = await conn.QueryAsync<(string Key, string Value)>(sql);
        return rows.ToDictionary(r => r.Key, r => r.Value);
    }
}
