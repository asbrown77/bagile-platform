using Dapper;
using Npgsql;

namespace Bagile.AcceptanceTests.Drivers;

/// <summary>
/// Helper for setting up and manipulating database test data.
/// </summary>
public class DatabaseDriver
{
    private readonly string _connectionString;

    public DatabaseDriver(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Deletes data from all key tables to ensure a clean state before each scenario.
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.ExecuteAsync(@"
            DELETE FROM bagile.enrolments;
            DELETE FROM bagile.students;
            DELETE FROM bagile.course_schedules;
            DELETE FROM bagile.orders;
            DELETE FROM bagile.raw_orders;
        ");
    }

    /// <summary>
    /// Inserts a new order and returns its generated internal ID.
    /// </summary>
    public async Task<long> InsertOrderAsync(OrderTestData order)
    {
        const string sql = @"
            INSERT INTO bagile.orders (
                external_id,
                source,
                type,
                status,
                total_amount,
                order_date,
                contact_email,
                billing_company
            )
            VALUES (
                @ExternalId,
                'woo',
                'public',
                @Status,
                @TotalAmount,
                @OrderDate,
                @CustomerEmail,
                @CustomerCompany
            )
            RETURNING id;
        ";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.ExecuteScalarAsync<long>(sql, order);
    }

    /// <summary>
    /// Inserts enrolment and ensures related student and course schedule records exist.
    /// </summary>
    public async Task InsertEnrolmentAsync(long orderId, EnrolmentTestData enrolment)
    {
        await using var conn = new NpgsqlConnection(_connectionString);

        var studentId = await conn.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.students (email, first_name, last_name)
            VALUES (@Email, 'Test', 'Student')
            ON CONFLICT (email) DO UPDATE SET email = EXCLUDED.email
            RETURNING id;
        ", new { Email = enrolment.StudentEmail });

        var courseId = await conn.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules (name, status, start_date, source_system, source_product_id)
            VALUES (@CourseName, 'published', NOW(), 'woo', 99999)
            ON CONFLICT (source_system, source_product_id) DO UPDATE SET name = EXCLUDED.name
            RETURNING id;
        ", new { CourseName = enrolment.CourseName });

        await conn.ExecuteAsync(@"
            INSERT INTO bagile.enrolments (student_id, order_id, course_schedule_id)
            VALUES (@StudentId, @OrderId, @CourseId)
            ON CONFLICT (student_id, order_id, course_schedule_id) DO NOTHING;
        ", new { StudentId = studentId, OrderId = orderId, CourseId = courseId });
    }

    /// <summary>
    /// Returns the internal order ID for a given external ID.
    /// </summary>
    public async Task<long> GetOrderIdByExternalIdAsync(string externalId)
    {
        const string sql = @"
            SELECT id
            FROM bagile.orders
            WHERE external_id = @ExternalId
            LIMIT 1;
        ";

        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QuerySingleAsync<long>(sql, new { ExternalId = externalId });
    }
}
