using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories
{
    public class RawRefundRepository : IRawRefundRepository
    {
        private readonly string _conn;
        public RawRefundRepository(string conn) => _conn = conn;

        public async Task InsertAsync(
            long wooOrderId,
            long refundId,
            decimal refundTotal,
            string? refundReason,
            string lineItemsJson,
            string rawJson,
            CancellationToken token)
        {
            const string sql = @"
                INSERT INTO bagile.raw_refunds
                    (woo_order_id, refund_id, refund_total, refund_reason, line_items, raw_json)
                VALUES
                    (@wooOrderId, @refundId, @refundTotal, @refundReason, @lineItems, @rawJson)
                ON CONFLICT (woo_order_id, refund_id) DO UPDATE
                    SET refund_total = EXCLUDED.refund_total,
                        refund_reason = EXCLUDED.refund_reason,
                        line_items = EXCLUDED.line_items,
                        raw_json = EXCLUDED.raw_json;";

            await using var conn = new NpgsqlConnection(_conn);
            await conn.ExecuteAsync(sql, new
            {
                wooOrderId,
                refundId,
                refundTotal,
                refundReason,
                lineItems = lineItemsJson,
                rawJson
            });
        }

        public async Task<IEnumerable<RawRefund>> GetByWooOrderIdAsync(long wooOrderId, CancellationToken token)
        {
            const string sql = @"
                SELECT id, woo_order_id AS WooOrderId, refund_id AS RefundId,
                       refund_total AS RefundTotal, refund_reason AS RefundReason,
                       line_items AS LineItemsJson, raw_json AS RawJson,
                       created_at AS CreatedAt
                FROM bagile.raw_refunds
                WHERE woo_order_id = @wooOrderId;";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.QueryAsync<RawRefund>(sql, new { wooOrderId });
        }

        public async Task<decimal> GetTotalRefundForWooOrderAsync(long wooOrderId, CancellationToken token)
        {
            const string sql = @"SELECT COALESCE(SUM(refund_total), 0) FROM bagile.raw_refunds WHERE woo_order_id = @wooOrderId;";
            await using var conn = new NpgsqlConnection(_conn);
            return await conn.ExecuteScalarAsync<decimal>(sql, new { wooOrderId });
        }
    }
}
