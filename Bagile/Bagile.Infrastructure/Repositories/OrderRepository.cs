using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

namespace Bagile.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _conn;

        public OrderRepository(string conn)
        {
            _conn = conn;
        }

        public async Task<Order?> GetByExternalIdAsync(string externalId)
        {
            using var db = new NpgsqlConnection(_conn);
            return await db.QuerySingleOrDefaultAsync<Order>(
                @"SELECT * FROM bagile.orders WHERE external_id = @externalId",
                new {externalId});
        }

        public async Task<long> UpsertOrderAsync(Order order)
        {
            const string sql = @"
        INSERT INTO bagile.orders (
            raw_order_id,
            external_id,
            source,
            type,
            reference,
            billing_company,
            contact_name,
            contact_email,
            total_quantity,
            sub_total,
            total_tax,
            total_amount,
            payment_total,
            refund_total,
            net_total,
            status,
            lifecycle_status,
            currency,
            order_date
        )
        VALUES (
            @RawOrderId,
            @ExternalId,
            @Source,
            @Type,
            @Reference,
            @BillingCompany,
            @ContactName,
            @ContactEmail,
            @TotalQuantity,
            @SubTotal,
            @TotalTax,
            @TotalAmount,
            @PaymentTotal,
            @RefundTotal,
            @NetTotal,
            @Status,
            @LifecycleStatus,
            @Currency,
            @OrderDate
        )
        ON CONFLICT (source, external_id) DO UPDATE
        SET
            billing_company   = EXCLUDED.billing_company,
            contact_name      = EXCLUDED.contact_name,
            contact_email     = EXCLUDED.contact_email,
            total_quantity    = EXCLUDED.total_quantity,
            sub_total         = EXCLUDED.sub_total,
            total_tax         = EXCLUDED.total_tax,
            total_amount      = EXCLUDED.total_amount,
            payment_total     = EXCLUDED.payment_total,
            refund_total      = EXCLUDED.refund_total,
            net_total         = EXCLUDED.net_total,
            status            = EXCLUDED.status,
            lifecycle_status  = EXCLUDED.lifecycle_status,
            currency          = EXCLUDED.currency,
            order_date        = EXCLUDED.order_date
        RETURNING id;";

            await using var conn = new NpgsqlConnection(_conn);
            return await conn.ExecuteScalarAsync<long>(sql, order);
        }


        public async Task UpdateOrderStatusAsync(long orderId, string status)
        {
            using var db = new NpgsqlConnection(_conn);
            await db.ExecuteAsync(
                "UPDATE bagile.orders SET status = @status WHERE id = @orderId",
                new {orderId, status});
        }
    }
}
