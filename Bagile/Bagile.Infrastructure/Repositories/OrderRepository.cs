using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Npgsql;

namespace Bagile.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly string _connectionString;

    public OrderRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task UpsertOrderAsync(Order order, CancellationToken token)
    {
        const string sql = @"
            INSERT INTO bagile.orders 
                (raw_order_id, external_id, source, type, billing_company, reference, contact_name, contact_email, total_amount, status, order_date)
            VALUES 
                (@rawOrderId, @externalId, @source, @type, @billingCompany, @reference, @contactName, @contactEmail, @total, @status, @orderDate)
            ON CONFLICT (external_id) DO UPDATE 
                SET billing_company = EXCLUDED.billing_company,
                    contact_name = EXCLUDED.contact_name,
                    contact_email = EXCLUDED.contact_email,
                    total_amount = EXCLUDED.total_amount,
                    status = EXCLUDED.status,
                    order_date = EXCLUDED.order_date,
                    updated_at = NOW();";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(token);

        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("rawOrderId", order.RawOrderId);
        cmd.Parameters.AddWithValue("externalId", order.ExternalId);
        cmd.Parameters.AddWithValue("source", order.Source);
        cmd.Parameters.AddWithValue("type", order.Type);
        cmd.Parameters.AddWithValue("reference", (object?)order.Reference ?? DBNull.Value);
        cmd.Parameters.AddWithValue("billingCompany", (object?)order.BillingCompany ?? DBNull.Value);
        cmd.Parameters.AddWithValue("contactName", (object?)order.ContactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("contactEmail", (object?)order.ContactEmail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("total", order.TotalAmount);
        cmd.Parameters.AddWithValue("status", (object?)order.Status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("orderDate", (object?)order.OrderDate ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(token);
    }
}
