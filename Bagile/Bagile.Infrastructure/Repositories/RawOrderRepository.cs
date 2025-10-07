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

        public async Task<bool> ExistsAsync(string source, string externalId, string payloadJson)
        {
            var hash = ComputeSha256(payloadJson);

            const string sql = @"
            SELECT 1
            FROM raw_orders
            WHERE source = @source
              AND external_id = @externalId
              AND payload_hash = @hash
            LIMIT 1;";

            await using var conn = new NpgsqlConnection(_connectionString);
            var result = await conn.ExecuteScalarAsync<int?>(sql, new { source, externalId, hash });
            return result.HasValue;
        }

        public async Task<DateTime?> GetLastTimestampAsync(string source)
        {
            string sql = source switch
            {
                "xero" => @"
            SELECT MAX( (payload->>'UpdatedDateUTC')::timestamp )
            FROM raw_orders
            WHERE source = @source
              AND payload ? 'UpdatedDateUTC';",

                _ => @"
            SELECT MAX(imported_at)
            FROM raw_orders
            WHERE source = @source;"
            };

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<DateTime?>(sql, new { source });
        }


        public async Task InsertIfChangedAsync(string source, string externalId, string payloadJson, string eventType)
        {
            if (await ExistsAsync(source, externalId, payloadJson))
                return; // skip identical payload

            var hash = ComputeSha256(payloadJson);

            const string sql = @"
            INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, payload_hash)
            VALUES (@source, @externalId, @payloadJson::jsonb, @eventType, @hash);";

            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.ExecuteAsync(sql, new { source, externalId, payloadJson, eventType, hash });
        }

        public async Task<int> InsertAsync(string source, string externalId, string payloadJson, string eventType)
        {
            const string sql = @"
                INSERT INTO bagile.raw_orders (source, external_id, payload, event_type)
                VALUES (@source, @externalId, @payloadJson::jsonb, @eventType)
                RETURNING id;";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(sql, new { source, externalId, payloadJson, eventType });
        }

        public async Task<IEnumerable<RawOrder>> GetAllAsync()
        {
            const string sql = @"
                SELECT id,
                       source,
                       external_id AS ExternalId,
                       payload::text AS Payload,
                       imported_at AS ImportedAt,
                       event_type AS EventType
                FROM raw_orders
                ORDER BY imported_at DESC;";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryAsync<RawOrder>(sql);
        }
    }
}