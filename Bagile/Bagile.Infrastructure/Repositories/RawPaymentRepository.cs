using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories
{
    public class RawPaymentRepository : IRawPaymentRepository
    {
        private readonly string _conn;
        public RawPaymentRepository(string conn) => _conn = conn;

        public async Task InsertAsync(RawPayment payment, CancellationToken token)
        {
            const string sql = @"
                INSERT INTO bagile.raw_payments
                    (order_id, source, amount, currency, raw_json)
                VALUES
                    (@OrderId, @Source, @Amount, @Currency, @RawJson);";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.ExecuteAsync(sql, payment);
        }

        public async Task<IEnumerable<RawPayment>> GetByOrderIdAsync(long orderId, CancellationToken token)
        {
            const string sql = @"
                SELECT id, order_id AS OrderId, source, amount,
                       currency, raw_json AS RawJson, created_at AS CreatedAt
                FROM bagile.raw_payments
                WHERE order_id = @orderId;";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.QueryAsync<RawPayment>(sql, new { orderId });
        }

        public async Task<decimal> GetTotalPaymentForOrderAsync(long orderId, CancellationToken token)
        {
            const string sql = @"SELECT COALESCE(SUM(amount), 0) FROM bagile.raw_payments WHERE order_id = @orderId;";
            await using var conn = new NpgsqlConnection(_conn);
            return await conn.ExecuteScalarAsync<decimal>(sql, new { orderId });
        }
    }
}