using Bagile.Domain.Repositories;
using Bagile.EtlService.Mappers;
using Bagile.EtlService.Services;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Repositories;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;
using System.Text.Json;
using Bagile.Infrastructure.Models;

namespace Bagile.IntegrationTests.EtlService;

[TestFixture]
[Category("Integration")]
public class TransferOrderBugTests
{
    private string _connectionString = null!;
    private NpgsqlConnection _db = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _connectionString = DatabaseFixture.ConnectionString;
        _db = new NpgsqlConnection(_connectionString);
        await _db.OpenAsync();

        Console.WriteLine("Setting up test data...");

        // Clean up test data
        await _db.ExecuteAsync("DELETE FROM bagile.enrolments WHERE 1=1;");
        await _db.ExecuteAsync("DELETE FROM bagile.students WHERE email LIKE '%test-transfer%';");
        await _db.ExecuteAsync("DELETE FROM bagile.orders WHERE external_id LIKE 'TEST-%' OR external_id = '99999';");
        await _db.ExecuteAsync("DELETE FROM bagile.raw_orders WHERE external_id LIKE 'TEST-%';");

        Console.WriteLine("Cleanup complete.");

        // Check if course schedule already exists
        var existing = await _db.QueryFirstOrDefaultAsync<long?>(
            "SELECT id FROM bagile.course_schedules WHERE source_product_id = 11840");

        if (existing.HasValue)
        {
            Console.WriteLine($"Course schedule already exists with id={existing.Value}");
        }
        else
        {
            Console.WriteLine("Inserting test course schedule...");

            // Insert a test course schedule that transfer orders will reference
            var inserted = await _db.ExecuteScalarAsync<long?>(@"
                INSERT INTO bagile.course_schedules 
                    (name, sku, source_system, source_product_id, start_date, status)
                VALUES 
                    ('Test PSPO Course', 'PSPO-010125-AB', 'woo', 11840, '2025-01-01', 'publish')
                RETURNING id;
            ");

            Console.WriteLine($"Inserted course schedule with id={inserted}");
        }

        // Verify it exists
        var verify = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id, name, sku, source_product_id FROM bagile.course_schedules WHERE source_product_id = 11840");

        if (verify == null)
        {
            throw new Exception("Failed to create or find course schedule for product_id 11840");
        }

        Console.WriteLine($"Verified: id={verify.id}, name={verify.name}, sku={verify.sku}, product_id={verify.source_product_id}");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _db?.Dispose();
    }

    [Test]
    [Order(1)]
    public async Task Prerequisite_CourseScheduleExists()
    {
        // Arrange & Act
        var schedule = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM bagile.course_schedules WHERE source_product_id = 11840");

        // Assert
        
        Assert.That(schedule, Is.Not.Null, "the test course schedule should exist");

        long actualProductId = schedule.source_product_id;
        actualProductId.Should().Be(11840, "course schedule should have the correct product ID");
    }

    [Test]
    [Order(2)]
    public void Bug_MapTickets_ReturnsEmpty_ForTransferOrders()
    {
        // Arrange - Real transfer order JSON (simplified)
        var transferOrderJson = @"{
            ""id"": 12345,
            ""status"": ""completed"",
            ""currency"": ""GBP"",
            ""billing"": {
                ""first_name"": ""John"",
                ""last_name"": ""Doe"",
                ""email"": ""john@test-transfer.com"",
                ""company"": ""Test Corp""
            },
            ""line_items"": [
                {
                    ""id"": 1,
                    ""product_id"": 11840,
                    ""name"": ""PSPO - 1-2 Jan 2025"",
                    ""sku"": ""PSPO-010125-AB"",
                    ""quantity"": 1,
                    ""price"": ""950""
                }
            ],
            ""meta_data"": []
        }";

        // Act - Call MapTickets
        var tickets = WooOrderTicketMapper.MapTickets(transferOrderJson);

        // Assert - BUG: Returns empty because no WooCommerceEventsOrderTickets
        tickets.Should().BeEmpty("this is the bug - transfer orders don't have ticket metadata");
    }

    [Test]
    [Order(3)]
    public void Comparison_MapTickets_ReturnsTickets_ForNormalOrders()
    {
        // Arrange - Normal order with tickets
        var normalOrderJson = @"{
            ""id"": 12243,
            ""status"": ""completed"",
            ""currency"": ""GBP"",
            ""billing"": {
                ""first_name"": ""Henry"",
                ""last_name"": ""Heselden"",
                ""email"": ""henry.heselden@themdu.com""
            },
            ""line_items"": [
                {
                    ""id"": 1,
                    ""product_id"": 11840,
                    ""name"": ""PSPO - 1-2 Jan 2025"",
                    ""quantity"": 2
                }
            ],
            ""meta_data"": [
                {
                    ""id"": 478405,
                    ""key"": ""WooCommerceEventsOrderTickets"",
                    ""value"": {
                        ""1"": {
                            ""1"": {
                                ""WooCommerceEventsAttendeeName"": ""Khalil"",
                                ""WooCommerceEventsAttendeeLastName"": ""Nazir"",
                                ""WooCommerceEventsAttendeeEmail"": ""Khalil.Nazir@themdu.com"",
                                ""WooCommerceEventsProductID"": ""11840""
                            },
                            ""2"": {
                                ""WooCommerceEventsAttendeeName"": ""Abiodun"",
                                ""WooCommerceEventsAttendeeLastName"": ""Odunlami"",
                                ""WooCommerceEventsAttendeeEmail"": ""Abiodun.Odunlami@themdu.com"",
                                ""WooCommerceEventsProductID"": ""11840""
                            }
                        }
                    }
                }
            ]
        }";

        // Act
        var tickets = WooOrderTicketMapper.MapTickets(normalOrderJson);

        // Assert - Works fine for normal orders
        tickets.Should().HaveCount(2, "normal orders have ticket metadata");
        tickets.First().Email.Should().Be("Khalil.Nazir@themdu.com");
    }

    [Test]
    [Order(4)]
    public void DataCheck_TransferOrdersHaveUsableData()
    {
        // Arrange
        var transferOrderJson = @"{
            ""id"": 12345,
            ""status"": ""completed"",
            ""billing"": {
                ""email"": ""john@test-transfer.com"",
                ""first_name"": ""John"",
                ""last_name"": ""Doe""
            },
            ""line_items"": [
                {
                    ""product_id"": 11840,
                    ""sku"": ""PSPO-010125-AB"",
                    ""name"": ""PSPO Course"",
                    ""quantity"": 1
                }
            ]
        }";

        using var doc = JsonDocument.Parse(transferOrderJson);
        var root = doc.RootElement;

        // Assert - All the data we need IS present
        root.TryGetProperty("billing", out var billing).Should().BeTrue();
        billing.TryGetProperty("email", out var email).Should().BeTrue();
        email.GetString().Should().Be("john@test-transfer.com");

        root.TryGetProperty("line_items", out var items).Should().BeTrue();
        var item = items.EnumerateArray().First();
        item.TryGetProperty("product_id", out var productId).Should().BeTrue();
        productId.GetInt64().Should().Be(11840);
        item.TryGetProperty("sku", out var sku).Should().BeTrue();
        sku.GetString().Should().Be("PSPO-010125-AB");
    }

    [Test]
    [Order(5)]
    public async Task EndToEnd_TransferOrderCreatesZeroEnrolments()
    {
        // Arrange - Create a transfer order with proper structure
        var transferOrderJson = @"{
            ""id"": 99999,
            ""status"": ""completed"",
            ""total"": ""950"",
            ""total_tax"": ""0"",
            ""currency"": ""GBP"",
            ""date_created"": ""2025-01-01T10:00:00"",
            ""number"": ""99999"",
            ""billing"": {
                ""first_name"": ""Jane"",
                ""last_name"": ""Transfer"",
                ""email"": ""jane@test-transfer.com"",
                ""company"": ""Transfer Test Corp""
            },
            ""line_items"": [
                {
                    ""id"": 1,
                    ""product_id"": 11840,
                    ""name"": ""PSPO - 1-2 Jan 2025"",
                    ""sku"": ""PSPO-010125-AB"",
                    ""quantity"": 1,
                    ""price"": ""950"",
                    ""total"": ""950""
                }
            ],
            ""meta_data"": []
        }";

        // Insert raw order
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, status)
            VALUES ('woo', 'TEST-99999', @payload::jsonb, 'import', 'pending')
            ON CONFLICT DO NOTHING
        ", new { payload = transferOrderJson });

        var rawOrder = await _db.QueryFirstAsync<dynamic>(
            "SELECT * FROM bagile.raw_orders WHERE external_id = 'TEST-99999'");

        // Act - Process with RawOrderTransformer
        var transformer = CreateRawOrderTransformer();
        await transformer.ProcessPendingAsync(CancellationToken.None);

        // Assert - Check if order was created
        var order = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM bagile.orders WHERE external_id = '99999'");

        if (order == null)
        {
            // Check if raw order was processed
            var rawOrderStatus = await _db.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT status, processed_at, error_message FROM bagile.raw_orders WHERE external_id = 'TEST-99999'");

            Assert.Fail($"Order was not created. RawOrder status: {rawOrderStatus?.status}, " +
                       $"processed_at: {rawOrderStatus?.processed_at}, error: {rawOrderStatus?.error_message}");
        }

        var orderId = (long)order.id;

        var enrolmentCount = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM bagile.enrolments WHERE order_id = @orderId",
            new { orderId });

        // Check what actually happened
        var students = await _db.QueryAsync<dynamic>(
            "SELECT * FROM bagile.students WHERE email LIKE '%test-transfer%'");

        var enrolments = await _db.QueryAsync<dynamic>(
            "SELECT * FROM bagile.enrolments WHERE order_id = @orderId",
            new { orderId });

        var courseSchedule = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM bagile.course_schedules WHERE source_product_id = 11840");

        Console.WriteLine($"Order created: ID={orderId}");
        Console.WriteLine($"Students created: {students.Count()}");
        Console.WriteLine($"Enrolments created: {enrolmentCount}");
        Console.WriteLine($"Test course schedule id: {courseSchedule?.id}");

        enrolmentCount.Should().Be(1);

    }

    private RawOrderTransformer CreateRawOrderTransformer()
    {
        var orderRepo = new OrderRepository(_connectionString);
        var rawOrderRepo = new RawOrderRepository(_connectionString);
        var studentRepo = new StudentRepository(_connectionString);
        var enrolmentRepo = new EnrolmentRepository(_connectionString);
        var courseRepo = new CourseScheduleRepository(_connectionString);

        // Mock FooEventsTicketsClient to return empty (simulating no API tickets)
        var mockFooEvents = new Mock<IFooEventsTicketsClient>();
        mockFooEvents
            .Setup(x => x.FetchTicketsForOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FooEventTicketDto>());

        // Mock XeroApiClient (not used in this test)
        var mockXero = new Mock<IXeroApiClient>();

        return new RawOrderTransformer(
            orderRepo,
            rawOrderRepo,
            studentRepo,
            enrolmentRepo,
            courseRepo,
            mockFooEvents.Object,
            mockXero.Object,
            NullLogger<RawOrderTransformer>.Instance
        );
    }
}