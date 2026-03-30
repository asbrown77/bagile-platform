using System.Security.Cryptography;
using System.Text;
using Dapper;
using Npgsql;

namespace Bagile.Api.Services;

public record ApiKeyInfo(Guid Id, string OwnerEmail, string OwnerName);

public class ApiKeyValidator
{
    private readonly string _connectionString;

    public ApiKeyValidator(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<ApiKeyInfo?> ValidateAsync(string rawKey)
    {
        var hash = ComputeHash(rawKey);

        const string sql = @"
            SELECT id AS Id, owner_email AS OwnerEmail, owner_name AS OwnerName
            FROM bagile.api_keys
            WHERE key_hash = @hash AND is_active = TRUE
            LIMIT 1;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ApiKeyInfo>(sql, new { hash });
    }

    public async Task RecordUsageAsync(Guid keyId)
    {
        const string sql = "UPDATE bagile.api_keys SET last_used_at = NOW() WHERE id = @keyId;";
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(sql, new { keyId });
    }

    public static string ComputeHash(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static (string rawKey, string hash, string prefix) GenerateKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var rawKey = "bgl_" + Convert.ToBase64String(bytes)
            .Replace("+", "").Replace("/", "").Replace("=", "");
        var hash = ComputeHash(rawKey);
        var prefix = rawKey[..12];
        return (rawKey, hash, prefix);
    }
}
