using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Bagile.Infrastructure.Xero;

/// <summary>
/// Reads and writes the Xero token row from bagile.integration_tokens (source = 'xero').
/// This is the existing singleton token row created by the OAuth flow (V11 migration).
/// All token persistence for the API layer goes through this repository.
/// </summary>
public class XeroTokenRepository
{
    private readonly string _connectionString;

    public XeroTokenRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection string not configured.");
    }

    public async Task<XeroTokenRow?> GetAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT
                refresh_token   AS RefreshToken,
                access_token    AS AccessToken,
                expires_at      AS ExpiresAt
            FROM bagile.integration_tokens
            WHERE source = 'xero';";

        await using var db = new NpgsqlConnection(_connectionString);
        return await db.QuerySingleOrDefaultAsync<XeroTokenRow>(
            new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task SaveAsync(string accessToken, string refreshToken, DateTime expiresAt, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE bagile.integration_tokens
            SET access_token = @accessToken,
                refresh_token = @refreshToken,
                expires_at    = @expiresAt
            WHERE source = 'xero';";

        await using var db = new NpgsqlConnection(_connectionString);
        var rows = await db.ExecuteAsync(
            new CommandDefinition(sql, new { accessToken, refreshToken, expiresAt }, cancellationToken: ct));

        if (rows == 0)
            throw new InvalidOperationException(
                "Xero integration not initialised — no row with source='xero' in bagile.integration_tokens. " +
                "Complete the OAuth flow at GET /xero/connect first, or seed the row manually.");
    }
}

public record XeroTokenRow(string RefreshToken, string? AccessToken, DateTime? ExpiresAt);
