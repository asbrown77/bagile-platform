using Dapper;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;

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


        public async Task<bool> ExistsAsync(string source, string externalId, string payloadJson)
        {
            var hash = ComputeSha256(payloadJson);

            const string sql = @"
            SELECT 1
            FROM bagile.raw_orders
            WHERE source = @source
              AND external_id = @externalId
              AND payload_hash = @hash
            LIMIT 1;";

            await using var conn = new NpgsqlConnection(_connectionString);
            var result = await conn.ExecuteScalarAsync<int?>(sql, new { source, externalId, hash });
            return result.HasValue;
        }

        public async Task<IEnumerable<RawOrder>> GetUnprocessedAsync(int limit = 100)
        {
            const string sql = @"
            SELECT id,
                   source,
                   external_id AS ExternalId,
                   payload::text AS Payload,
                   event_type AS EventType,
                   payload_hash AS PayloadHash,
                   created_at AS CreatedAt,
                   processed_at AS ProcessedAt,
                   status
            FROM bagile.raw_orders
<<<<<<< HEAD
            WHERE status = 'pending'
              AND processed_at IS NULL
=======
            WHERE (processed_at IS NULL OR status = 'pending')
              AND (event_type = 'import' OR event_type IS NULL)
>>>>>>> ab3f69db0077185cff7b7866386f4d8978738edb
            ORDER BY created_at
            LIMIT @limit;";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryAsync<RawOrder>(sql, new { limit });
        }

        public async Task MarkProcessedAsync(long id)
        {
            const string sql = @"
            UPDATE bagile.raw_orders
            SET processed_at = NOW(),
                status = 'processed'
            WHERE id = @id;";

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteAsync(sql, new { id });
        }

        public async Task MarkFailedAsync(long id, string errorMessage)
        {
            const string sql = @"
            UPDATE bagile.raw_orders
            SET status = 'error',
                error_message = @errorMessage,  
                processed_at = NOW()
            WHERE id = @id;";

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteAsync(sql, new { id });
        }

        public async Task<int> InsertAsync(string source, string externalId, string payloadJson, string eventType)
        {
            var hash = ComputeSha256(payloadJson);

            const string sql = @"

            INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, payload_hash, status)
            VALUES (@source, @externalId, CAST(@payloadJson AS jsonb), @eventType, @hash, 'pending')
            ON CONFLICT (source, payload_hash) DO NOTHING
            RETURNING id;";


            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int?>(sql, new { source, externalId, payloadJson, eventType, hash }) ?? 0;
        }


        public async Task<int> InsertIfChangedAsync(string source, string externalId, string payloadJson, string eventType)
        {
            if (await ExistsAsync(source, externalId, payloadJson))
                return 0; // nothing inserted

            return await InsertAsync(source, externalId, payloadJson, eventType);
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
                    SELECT MAX(created_at)
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
                       created_at AS CreatedAt,
                       event_type AS EventType,
                       payload_hash AS PayloadHash
                FROM bagile.raw_orders
                ORDER BY created_at DESC;";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryAsync<RawOrder>(sql);
        }
    }
}
