using Bagile.Application.Common.Interfaces;
using Bagile.Application.Orders.DTOs;
using Dapper;
using Npgsql;
using System.Text.Json;

namespace Bagile.Infrastructure.Persistence.Queries;

public class OrderQueries : IOrderQueries
{
    private readonly string _connectionString;

    public OrderQueries(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IEnumerable<OrderDto>> GetOrdersAsync(
        string? status,
        DateTime? from,
        DateTime? to,
        string? email,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                o.id AS Id,
                o.source AS Source,
                o.external_id AS ExternalId,
                o.status AS Status,
                o.reference AS Reference,
                o.total_amount AS TotalAmount,
                o.order_date AS OrderDate,
                o.contact_name AS CustomerName,
                o.contact_email AS CustomerEmail,
                o.billing_company AS CustomerCompany
            FROM bagile.orders o
            WHERE 1=1
            " + (status != null ? " AND o.status = @status" : "") + @"
            " + (from != null ? " AND o.order_date >= @from" : "") + @"
            " + (to != null ? " AND o.order_date <= @to" : "") + @"
            " + (email != null ? " AND o.contact_email ILIKE @emailPattern" : "") + @"
            ORDER BY o.order_date DESC
            LIMIT @pageSize OFFSET @offset;";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryAsync<OrderDto>(sql, new
        {
            status,
            from,
            to,
            emailPattern = email != null ? $"%{email}%" : null,
            pageSize,
            offset = (page - 1) * pageSize
        });
    }

    public async Task<int> CountOrdersAsync(
        string? status,
        DateTime? from,
        DateTime? to,
        string? email,
        CancellationToken ct = default)
    {
        var sql = @"
            SELECT COUNT(*) 
            FROM bagile.orders o
            WHERE 1=1
            " + (status != null ? " AND o.status = @status" : "") + @"
            " + (from != null ? " AND o.order_date >= @from" : "") + @"
            " + (to != null ? " AND o.order_date <= @to" : "") + @"
            " + (email != null ? " AND o.contact_email ILIKE @emailPattern" : "");

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<int>(sql, new
        {
            status,
            from,
            to,
            emailPattern = email != null ? $"%{email}%" : null
        });
    }

    public async Task<OrderDetailDto?> GetOrderByIdAsync(long orderId, CancellationToken ct = default)
    {
        var sql = @"
            SELECT 
                o.id AS Id,
                o.source AS Source,
                o.external_id AS ExternalId,
                o.status AS Status,
                o.reference AS Reference,
                o.total_amount AS TotalAmount,
                o.order_date AS OrderDate,
                o.contact_name AS CustomerName,
                o.contact_email AS CustomerEmail,
                o.billing_company AS CustomerCompany,
                r.payload::text AS RawPayload
            FROM bagile.orders o
            LEFT JOIN bagile.raw_orders r ON o.raw_order_id = r.id
            WHERE o.id = @orderId;";

        await using var conn = new NpgsqlConnection(_connectionString);
        var order = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { orderId });

        if (order == null) return null;

        // Build OrderDetailDto
        var result = new OrderDetailDto
        {
            Id = order.id,
            Source = order.source,
            ExternalId = order.externalid,
            Status = order.status,
            Reference = order.reference,
            TotalAmount = order.totalamount,
            OrderDate = order.orderdate,
            CustomerName = order.customername,
            CustomerEmail = order.customeremail,
            CustomerCompany = order.customercompany,
            Customer = new CustomerInfo
            {
                Name = order.customername,
                Email = order.customeremail,
                Company = order.customercompany
            },
            Enrolments = await GetEnrolmentsForOrderAsync(conn, orderId),
            LineItems = ExtractLineItemsFromRawPayload(order.rawpayload)
        };

        return result;
    }

    private async Task<IEnumerable<EnrolmentDto>> GetEnrolmentsForOrderAsync(
        NpgsqlConnection conn,
        long orderId)
    {
        var sql = @"
        SELECT
            e.id AS EnrolmentId,
            e.status AS Status,
            s.email AS StudentEmail,
            CONCAT(s.first_name, ' ', s.last_name) AS StudentName,
            cs.name AS CourseName,
            cs.start_date AS CourseStartDate,
            e.original_sku IS NOT NULL AS IsTransfer,
            e.transferred_from_enrolment_id AS TransferredFromEnrolmentId,
            e.transferred_to_enrolment_id AS TransferredToEnrolmentId,
            e.original_sku AS OriginalSku,
            e.transfer_reason AS TransferReason,
            CASE
                WHEN e.transfer_reason = 'course_cancelled' THEN 'Course Cancelled by Provider'
                WHEN e.transfer_reason = 'attendee_requested' THEN 'Attendee Requested Transfer'
                ELSE NULL
            END AS TransferReasonLabel,
            e.refund_eligible AS RefundEligible,
            e.transfer_notes AS TransferNotes
        FROM bagile.enrolments e
        JOIN bagile.students s ON e.student_id = s.id
        LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
        WHERE e.order_id = @orderId
        ORDER BY e.created_at;";

        return await conn.QueryAsync<EnrolmentDto>(sql, new { orderId });
    }

    private List<LineItemDto> ExtractLineItemsFromRawPayload(string? rawPayload)
    {
        if (string.IsNullOrEmpty(rawPayload)) return new();

        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            if (!doc.RootElement.TryGetProperty("line_items", out var items))
                return new();

            return items.EnumerateArray()
                .Select(item => new LineItemDto
                {
                    ProductId = item.TryGetProperty("product_id", out var pid) ? pid.GetInt64() : 0,
                    Name = item.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                    Quantity = item.TryGetProperty("quantity", out var qty) ? qty.GetInt32() : 0,
                    Price = item.TryGetProperty("price", out var price) ? price.GetDecimal() : 0
                })
                .ToList();
        }
        catch
        {
            return new();
        }
    }
}