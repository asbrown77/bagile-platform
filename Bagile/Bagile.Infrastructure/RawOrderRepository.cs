using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure
{
    public class RawOrderRepository : IRawOrderRepository
    {
        private readonly string _connectionString;

        public RawOrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> InsertAsync(string source, string externalId, string payload, string? eventType = null)
        {
            const string sql = @"
                INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, received_at)
                VALUES (@Source, @ExternalId, CAST(@Payload AS jsonb), @EventType, NOW())
                RETURNING id;
            ";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                Source = source,
                ExternalId = externalId,
                Payload = payload,
                EventType = eventType
            });
        }

        public async Task<IEnumerable<RawOrder>> GetAllAsync()
        {
            const string sql = @"
                SELECT id,
                       source,
                       external_id AS ExternalId,
                       payload::text AS Payload,
                       received_at AS ReceivedAt,
                       event_type AS EventType
                FROM bagile.raw_orders
                ORDER BY received_at DESC;
            ";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryAsync<RawOrder>(sql);
        }
    }
}