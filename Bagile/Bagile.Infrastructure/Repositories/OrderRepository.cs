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

    public async Task UpsertOrderAsync(
        long rawOrderId,
        string externalId,
        string source,
        string type,
        string? billingCompany,
        string? contactName,
        string? contactEmail,
        decimal totalAmount,
        string? status,
        DateTime? orderDate,
        CancellationToken token)
    {
        const string sql = @"
            INSERT INTO bagile.orders 
                (raw_order_id, external_id, source, type, billing_company, contact_name, contact_email, total_amount, status, order_date)
            VALUES 
                (@rawOrderId, @externalId, @source, @type, @billingCompany, @contactName, @contactEmail, @total, @status, @orderDate)
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
        cmd.Parameters.AddWithValue("rawOrderId", rawOrderId);
        cmd.Parameters.AddWithValue("externalId", externalId);
        cmd.Parameters.AddWithValue("source", source);
        cmd.Parameters.AddWithValue("type", type);
        cmd.Parameters.AddWithValue("billingCompany", (object?)billingCompany ?? DBNull.Value);
        cmd.Parameters.AddWithValue("contactName", (object?)contactName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("contactEmail", (object?)contactEmail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("total", totalAmount);
        cmd.Parameters.AddWithValue("status", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("orderDate", (object?)orderDate ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(token);
    }
}
