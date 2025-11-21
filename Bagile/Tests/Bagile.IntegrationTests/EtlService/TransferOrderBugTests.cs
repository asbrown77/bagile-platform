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

    [Test]
    [Order(6)]
    public async Task Fix_TransferOrder_CourseScheduleIdPopulated_ViaSku()
    {
        // Arrange - Create transfer order with SKU that exists in DB
        var transferOrderJson = @"{
        ""id"": 88888,
        ""status"": ""completed"",
        ""total"": ""950"",
        ""total_tax"": ""0"",
        ""currency"": ""GBP"",
        ""date_created"": ""2025-01-01T10:00:00"",
        ""number"": ""88888"",
        ""billing"": {
            ""first_name"": ""Transfer"",
            ""last_name"": ""Test"",
            ""email"": ""transfer-fix@test.com"",
            ""company"": ""Fix Test Corp""
        },
        ""line_items"": [
            {
                ""id"": 1,
                ""product_id"": 99999,
                ""name"": ""PSPO Course"",
                ""sku"": ""PSPO-010125-AB"",
                ""quantity"": 1,
                ""price"": ""950""
            }
        ],
        ""meta_data"": []
    }";

        await _db.ExecuteAsync(@"
        INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, status)
        VALUES ('woo', 'TEST-88888', @payload::jsonb, 'import', 'pending')
        ON CONFLICT DO NOTHING
    ", new { payload = transferOrderJson });

        var transformer = CreateRawOrderTransformer();

        // Act
        await transformer.ProcessPendingAsync(CancellationToken.None);

        // Assert - Order created
        var order = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM bagile.orders WHERE external_id = '88888'");

        Assert.That(order, Is.Not.Null, "order should be created");

        var orderId = (long)order.id;

        // Assert - Enrolment created WITH course_schedule_id
        var enrolments = await _db.QueryAsync<dynamic>(
            "SELECT * FROM bagile.enrolments WHERE order_id = @orderId",
            new { orderId });

        enrolments.Should().HaveCount(1, "one enrolment should be created");

        var enrolment = enrolments.First();

        // ✅ THE FIX: course_schedule_id should NOT be null
        Assert.That(enrolment.course_schedule_id, Is.Not.Null,
            "course_schedule_id should be resolved via SKU lookup");

        // Verify it's the correct course schedule
        var courseSchedule = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM bagile.course_schedules WHERE id = @id",
            new { id = enrolment.course_schedule_id });

        Assert.That(courseSchedule, Is.Not.Null);
        Assert.That(courseSchedule.sku, Is.EqualTo("PSPO-010125-AB"));

    }

    [Test]
    [Order(7)]
    public async Task Fix_TransferOrder_WithInvalidSku_GracefullyHandles()
    {
        // Arrange - Transfer order with SKU that DOESN'T exist
        var transferOrderJson = @"{
        ""id"": 77777,
        ""status"": ""completed"",
        ""total"": ""950"",
        ""currency"": ""GBP"",
        ""date_created"": ""2025-01-01T10:00:00"",
        ""billing"": {
            ""email"": ""invalid-sku@test.com"",
            ""first_name"": ""Invalid"",
            ""last_name"": ""Sku""
        },
        ""line_items"": [
            {
                ""product_id"": 88888,
                ""sku"": ""INVALID-SKU-999"",
                ""quantity"": 1
            }
        ],
        ""meta_data"": []
    }";

        await _db.ExecuteAsync(@"
        INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, status)
        VALUES ('woo', 'TEST-77777', @payload::jsonb, 'import', 'pending')
    ", new { payload = transferOrderJson });

        var transformer = CreateRawOrderTransformer();

        // Act
        await transformer.ProcessPendingAsync(CancellationToken.None);

        // Assert - Order created
        var order = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM bagile.orders WHERE external_id = '77777'");

        Assert.That(order, Is.Not.Null);
        var orderId = (long)order.id;

        // Enrolment created but with NULL course_schedule_id (graceful degradation)
        var enrolments = await _db.QueryAsync<dynamic>(
            "SELECT * FROM bagile.enrolments WHERE order_id = @orderId",
            new { orderId });

        enrolments.Should().HaveCount(1, "enrolment still created even with invalid SKU");

        var enrolment = enrolments.First();
        Assert.That(enrolment.course_schedule_id, Is.Null,
            "course_schedule_id should be null when SKU not found in DB");
    }

    [Test]
    [Order(8)]
    public async Task Fix_NormalOrder_WithProductId_StillWorks()
    {
        // Arrange - Ensure normal orders with valid product_id still work
        var scheduleId = await _db.ExecuteScalarAsync<long>(@"
        INSERT INTO bagile.course_schedules 
            (name, sku, source_system, source_product_id, start_date, status)
        VALUES 
            ('Normal Order Test', 'NORMAL-SKU', 'woo', 55555, '2025-01-01', 'publish')
        RETURNING id;
    ");

        var normalOrderJson = @"{
        ""id"": 66666,
        ""status"": ""completed"",
        ""currency"": ""GBP"",
        ""date_created"": ""2025-01-01T10:00:00"",
        ""billing"": {
            ""email"": ""normal@test.com"",
            ""first_name"": ""Normal"",
            ""last_name"": ""Order""
        },
        ""line_items"": [
            {
                ""product_id"": 55555,
                ""sku"": ""NORMAL-SKU"",
                ""quantity"": 1
            }
        ],
        ""meta_data"": []
    }";

        await _db.ExecuteAsync(@"
        INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, status)
        VALUES ('woo', 'TEST-66666', @payload::jsonb, 'import', 'pending')
    ", new { payload = normalOrderJson });

        var transformer = CreateRawOrderTransformer();

        // Act
        await transformer.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var order = await _db.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM bagile.orders WHERE external_id = '66666'");

        var orderId = (long)order.id;

        var enrolments = await _db.QueryAsync<dynamic>(
            "SELECT * FROM bagile.enrolments WHERE order_id = @orderId",
            new { orderId });

        enrolments.Should().HaveCount(1);

        var enrolment = enrolments.First();

        // Should resolve via product_id (original path)
        var courseScheduleId = (long)enrolment.course_schedule_id;
        courseScheduleId.Should().Be(scheduleId,
            "normal orders should still resolve via product_id");
    }

    [Test]
    public async Task Transfer_Order_Should_Create_Transfer_Enrolment_But_Does_Not()
    {
        // Unique IDs prevent pollution from previous tests
        var transferExternalId = $"TEST-{Guid.NewGuid()}";
        var oldExternalId = $"TEST-OLD-{Guid.NewGuid()}";

        // Arrange old schedule
        var oldScheduleId = await SeedCourseScheduleAsync(
            sku: "PSPO-071025-AB",
            startDate: new DateTime(2025, 10, 7)
        );

        // Arrange new schedule for transfer
        var newScheduleId = await SeedCourseScheduleAsync(
            sku: "PSPOA-241125-AB",
            startDate: new DateTime(2025, 11, 24)
        );

        var studentId = await SeedStudentAsync(
            email: "transfer@test.com",
            first: "Chris",
            last: "Transfer",
            company: "Bagile Ltd"
        );

        var oldOrderId = await SeedOrderAsync(
            externalId: oldExternalId,
            company: "Bagile Ltd",
            email: "transfer@test.com",
            createdAt: new DateTime(2025, 10, 7)
        );

        await SeedEnrolmentAsync(
            orderId: oldOrderId,
            studentId: studentId,
            scheduleId: oldScheduleId
        );

        // Minimal transfer JSON with UNIQUE external id
        var payload = $@"
        {{
            ""id"": ""{transferExternalId}"",
            ""number"": ""{transferExternalId}"",
            ""status"": ""completed"",
            ""billing"": {{
                ""email"": ""transfer@test.com"",
                ""first_name"": ""Chris"",
                ""last_name"": ""Transfer"",
                ""company"": ""Bagile Ltd""
            }},
            ""line_items"": [
                {{
                    ""sku"": ""PSPOA-241125-AB"",
                    ""quantity"": 1
                }}
            ],
            ""meta_data"": []
        }}";


        // Insert raw transfer order
        await _db.ExecuteAsync(@"
        INSERT INTO bagile.raw_orders
        (source, external_id, payload, event_type, status)
        VALUES ('woo', @extId, @p::jsonb, 'import', 'pending')
    ", new { extId = transferExternalId, p = payload });

        // Act
        await CreateRawOrderTransformer().ProcessPendingAsync(CancellationToken.None);

        // Assert
        var rows = (await _db.QueryAsync<dynamic>(@"
        SELECT e.*
        FROM bagile.enrolments e
        JOIN bagile.orders o ON o.id = e.order_id
        WHERE o.external_id IN (@oldExtId, @newExtId)
        ORDER BY e.id
    ", new { oldExtId = oldExternalId, newExtId = transferExternalId })).ToList();

        rows.Should().HaveCount(2, "a new transfer enrolment should be created but isn't");
    }

    [Test]
    public async Task Transfer_Order_Should_Create_Transfer_Enrolment_And_Link_To_Original()
    {
        // Arrange. Seed ORIGINAL enrolment on old order 12096
        var oldScheduleId = await SeedCourseScheduleAsync(
            sku: "PSPOA-071025-CB",
            startDate: new DateTime(2025, 10, 7)
        );

        var studentId = await SeedStudentAsync(
            email: "chris@test-transfer.com",
            first: "Chris",
            last: "Transfer",
            company: "BAgile Limited"
        );

        var oldOrderId = await SeedOrderAsync(
            externalId: "12096",
            company: "BAgile Limited",
            email: "chris@test-transfer.com",
            createdAt: new DateTime(2025, 10, 7, 13, 0, 0)
        );

        var oldEnrolmentId = await SeedEnrolmentAsync(
            orderId: oldOrderId,
            studentId: studentId,
            scheduleId: oldScheduleId
        );

        // IMPORTANT: Seed schedule that matches the transfer order SKU
        // From woo-order-12164.json -> "sku": "PSPOA-241125-AB"
        var transferScheduleId = await SeedCourseScheduleAsync(
            sku: "PSPOA-241125-AB",
            startDate: new DateTime(2025, 11, 24)
        );

        // Arrange. Insert raw transfer order from real Woo JSON
        var jsonPath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "woo-order-12164.json"
        );

        var rawJson = File.ReadAllText(jsonPath)
            .Replace("\"id\": 12164", "\"id\": 99999")
            .Replace("chrisbexon@bagile.co.uk", "chris@test-transfer.com");

        await _db.ExecuteAsync(@"
        INSERT INTO bagile.raw_orders (source, external_id, payload, event_type, status)
        VALUES ('woo', '12164', @payload::jsonb, 'import', 'pending')
        ON CONFLICT DO NOTHING;
    ", new { payload = rawJson });

        // Act
        var transformer = CreateRawOrderTransformer();
        await transformer.ProcessPendingAsync(CancellationToken.None);

        // Assert
        var rows = (await _db.QueryAsync<dynamic>(@"
        SELECT 
            o.external_id,
            e.id,
            e.status,
            e.transferred_from_enrolment_id,
            e.transferred_to_enrolment_id,
            e.course_schedule_id
        FROM bagile.orders o
        JOIN bagile.enrolments e ON e.order_id = o.id
        WHERE o.external_id IN ('12096','12164')
        ORDER BY o.external_id, e.id;
    ")).ToList();

        rows.Should().HaveCount(2);

        var original = rows.Single(r => r.external_id == "12096");
        var transfer = rows.Single(r => r.external_id == "12164");

        ((string)original.status).Should().Be("transferred");
        ((long?)original.transferred_to_enrolment_id).Should().NotBeNull();

        ((string)transfer.status).Should().Be("active");
        ((long?)transfer.transferred_from_enrolment_id).Should().Be((long?)original.id);

        // FIXED: This now passes because we seeded the correct schedule
        ((long?)transfer.course_schedule_id).Should().NotBeNull();
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

    //
    // SEED HELPERS
    //

    private async Task<long> SeedCourseScheduleAsync(string sku, DateTime startDate)
    {
        return await _db.ExecuteScalarAsync<long>(@"
        INSERT INTO bagile.course_schedules
            (name, sku, source_system, source_product_id, start_date, status)
        VALUES
            (@name, @sku, 'woo', FLOOR(RANDOM() * 90000 + 10000), @startDate, 'publish')
        RETURNING id;
    ", new
        {
            name = $"Seeded {sku}",
            sku,
            startDate
        });
    }

    private async Task<long> SeedStudentAsync(string email, string first, string last, string company)
    {
        return await _db.ExecuteScalarAsync<long>(@"
        INSERT INTO bagile.students (email, first_name, last_name, company)
        VALUES (@email, @first, @last, @company)
        ON CONFLICT (email) DO UPDATE
        SET first_name = EXCLUDED.first_name,
            last_name  = EXCLUDED.last_name,
            company    = EXCLUDED.company
        RETURNING id;
    ", new { email, first, last, company });
    }

    private async Task<long> SeedOrderAsync(
        string externalId,
        string company,
        string email,
        DateTime createdAt)
    {
        return await _db.ExecuteScalarAsync<long>(@"
        INSERT INTO bagile.orders
        (
            external_id,
            source,
            type,
            reference,
            billing_company,
            contact_email,
            total_amount,
            total_tax,
            sub_total,
            total_quantity,
            status,
            order_date,
            created_at,
            updated_at
        )
        VALUES
        (
            @externalId,
            'woo',
            'public',            -- REQUIRED
            NULL,
            @company,
            @Email,
            0,                    -- required NOT NULL
            0,                    -- required NOT NULL
            0,                    -- required NOT NULL
            1,                    -- required NOT NULL
            'completed',
            @CreatedAt,
            @CreatedAt,
            @CreatedAt
        )
        ON CONFLICT (external_id) DO UPDATE SET
            billing_company = EXCLUDED.billing_company,
            contact_email  = EXCLUDED.contact_email,
            order_date     = EXCLUDED.order_date,
            updated_at     = NOW()
        RETURNING id;
    ",
            new
            {
                externalId,
                Email = email,
                Company = company,
                CreatedAt = createdAt
            });
    }


    private async Task<long> SeedEnrolmentAsync(long orderId, long studentId, long scheduleId)
    {
        return await _db.ExecuteScalarAsync<long>(@"
        INSERT INTO bagile.enrolments
            (order_id, student_id, course_schedule_id, status, created_at)
        VALUES
            (@orderId, @studentId, @scheduleId, 'active', NOW())
        RETURNING id;
    ", new
        {
            orderId,
            studentId,
            scheduleId
        });
    }


}