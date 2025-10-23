using Bagile.Infrastructure.Models;
using Dapper;
using Npgsql;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bagile.Infrastructure.Repositories
{
    public class RawOrderRepository : IRawOrderRepository
    {
        private readonly string _connectionString;

        public RawOrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        public async Task<int> InsertAsync(string source, string externalId, string payloadJson, string eventType)
        {
            var hash = ComputeSha256(payloadJson);

            const string sql = @"
                INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, payload_hash)
                VALUES (@source, @externalId, CAST(@payloadJson AS jsonb), @eventType, @hash)
                ON CONFLICT (source, payload_hash) DO NOTHING
                RETURNING id;";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int?>(sql, new { source, externalId, payloadJson, eventType, hash }) ?? 0;
        }

        public async Task<DateTime?> GetLastTimestampAsync(string source)
        {
            string sql = source switch
            {
                "xero" => @"
                    SELECT MAX(
                        CASE
                            WHEN (payload->>'UpdatedDateUTC') ~ '^[0-9]{4}-'
                            THEN (payload->>'UpdatedDateUTC')::timestamp
                            ELSE NULL
                        END
                    )
                    FROM bagile.raw_orders
                    WHERE source = @source
                      AND payload ? 'UpdatedDateUTC';",

                _ => @"
                    SELECT MAX(imported_at)
                    FROM bagile.raw_orders
                    WHERE source = @source;"
            };

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<DateTime?>(sql, new { source });
        }

        public async Task<IEnumerable<RawOrder>> GetAllAsync()
        {
            const string sql = @"
                SELECT id,
                       source,
                       external_id AS ExternalId,
                       payload::text AS Payload,
                       imported_at AS ImportedAt,
                       event_type AS EventType,
                       payload_hash AS PayloadHash
                FROM bagile.raw_orders
                ORDER BY imported_at DESC;";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryAsync<RawOrder>(sql);
        }
    }
}
