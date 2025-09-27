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

        public async Task<int> InsertAsync(string source, string payload)
        {
            const string sql = @"
                INSERT INTO raw_orders (source, payload)
                VALUES (@Source, CAST(@Payload AS jsonb))
                RETURNING id;
            ";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.ExecuteScalarAsync<int>(sql, new { Source = source, Payload = payload });
        }

        public async Task<IEnumerable<RawOrder>> GetAllAsync()
        {
            const string sql = @"
                SELECT id, source, payload, imported_at
                FROM raw_orders
                ORDER BY imported_at DESC;
            ";

            await using var conn = new NpgsqlConnection(_connectionString);
            return await conn.QueryAsync<RawOrder>(sql);
        }
    }
}