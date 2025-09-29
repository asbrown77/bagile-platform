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

        public async Task<int> InsertAsync(string source, string externalId, string payload)
        {
            const string sql = @"
                INSERT INTO bagile.raw_orders (source, external_id, payload)
                VALUES (@Source, @ExternalId, CAST(@Payload AS jsonb))
                ON CONFLICT (source, external_id, payload) DO NOTHING
                RETURNING id;
                    ";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(sql, new { Source = source, ExternalId = externalId, Payload = payload });
        }

        public async Task<IEnumerable<RawOrder>> GetAllAsync()
        {
            const string sql = @"
                SELECT id,
                       source,
                       external_id AS ExternalId,
                       payload::text AS Payload,
                       imported_at AS ImportedAt
                FROM bagile.raw_orders
                ORDER BY imported_at DESC;
            ";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryAsync<RawOrder>(sql);
        }
    }
}