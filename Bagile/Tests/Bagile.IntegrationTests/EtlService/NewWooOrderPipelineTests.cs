using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Services;
using Bagile.EtlService.Models;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Repositories;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Bagile.IntegrationTests.EtlService;

[TestFixture]
[Category("Integration")]
public class NewWooOrderPipelineTests
{
    private string _conn = null!;
    private NpgsqlConnection _db = null!;
    private ILoggerFactory _log = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    [OneTimeSetUp]
    public async Task Setup()
    {
        _conn = DatabaseFixture.ConnectionString;
        _db = new NpgsqlConnection(_conn);
        await _db.OpenAsync();

        // Clean everything
        await _db.ExecuteAsync("DELETE FROM bagile.enrolments;");
        await _db.ExecuteAsync("DELETE FROM bagile.students WHERE email LIKE '%test%';");
        await _db.ExecuteAsync("DELETE FROM bagile.orders WHERE external_id LIKE 'TEST-%';");
        await _db.ExecuteAsync("DELETE FROM bagile.raw_orders WHERE external_id LIKE 'TEST-%';");

        // Seed schedule for enrolment link
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.course_schedules
            (name, sku, source_system, source_product_id, start_date, status)
            VALUES ('Test PSPO', 'PSPO-010125-AB', 'woo', 11840, '2025-01-01', 'publish')
            ON CONFLICT DO NOTHING;
        ");
    }

    [OneTimeTearDown]
    public void Teardown()
    {
        _db?.Dispose();
        _log.Dispose();
    }

    // ------------------------------------------------------------
    // TEST 1: Parser extracts billing fields only
    // ------------------------------------------------------------
    [Test]
    public async Task Parser_ExtractsBillingTicketData()
    {
        var json = @"{
            ""id"": 90001,
            ""billing"": {
                ""first_name"": ""John"",
                ""last_name"": ""Doe"",
                ""email"": ""john@test.com"",
                ""company"": ""Test Ltd""
            },
            ""line_items"": [
                { ""product_id"": 11840, ""sku"": ""PSPO-010125-AB"" }
            ]
        }";

        var parser = new WooOrderParser(Mock.Of<ILogger<WooOrderParser>>(), Mock.Of<IFooEventsTicketsClient>());
        var dto = await parser.Parse(
            new RawOrder { Id = 1, ExternalId = "TEST-90001", Payload = json, Source = "woo" }
        );

        dto.BillingEmail.Should().Be("john@test.com");
        dto.BillingName.Should().Be("John Doe");

        // No FooEvents ticket metadata → NO tickets
        dto.Tickets.Count.Should().Be(1);
    }

    // ------------------------------------------------------------
    // TEST 2: WooOrderService should create 1 enrolment
    // ------------------------------------------------------------
    [Test]
    public async Task WooOrderService_CreatesStudentAndEnrolment()
    {
        // Create REAL order (WooOrderService depends on it)
        long orderId = await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.orders
                (external_id, source, type, reference,
                 billing_company, contact_email,
                 total_amount, total_tax, sub_total,
                 total_quantity, status,
                 order_date, created_at, updated_at)
            VALUES
                ('TEST-SERVICE-1', 'woo', 'public', NULL,
                 'Test Co', 'order@test.com',
                 0, 0, 0,
                 1, 'completed',
                 NOW(), NOW(), NOW())
            RETURNING id;
        ");

        var dto = new CanonicalWooOrderDto
        {
            RawOrderId = 1,
            OrderId = orderId,
            BillingEmail = "order@test.com",
            BillingName = "Order Test",
            BillingCompany = "Test Co",
            RawPayload = @"{
                ""billing"": { ""email"": ""order@test.com"" },
                ""line_items"": [
                    { ""product_id"": 11840, ""sku"": ""PSPO-010125-AB"" }
                ]
            }",
            Tickets = new List<CanonicalTicketDto>(),
            HasFooEventsMetadata = false
        };

        var studentRepo = new StudentRepository(_conn);
        var enrolRepo = new EnrolmentRepository(_conn);
        var courseRepo = new CourseScheduleRepository(_conn);
        var orderRepo = new OrderRepository(_conn);

        var service = new WooOrderService(
            studentRepo,
            enrolRepo,
            courseRepo,
            orderRepo,
            _log.CreateLogger<WooOrderService>()
        );

        await service.ProcessAsync(dto, CancellationToken.None);

        var count = await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM bagile.enrolments WHERE order_id = @id",
            new { id = orderId }
        );

        count.Should().Be(1);
    }

    // ------------------------------------------------------------
    // TEST 3: RawOrderTransformer — full pipeline
    // ------------------------------------------------------------
    [Test]
    public async Task Transformer_EndToEnd_Works()
    {
        var json = @"{
            ""id"": 123456,
            ""billing"": { ""email"": ""transform@test.com"" },
            ""line_items"": [
                { ""product_id"": 11840, ""sku"": ""PSPO-010125-AB"" }
            ]
        }";

        // Insert raw order
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.raw_orders
                (source, external_id, payload, event_type, status)
            VALUES
                ('woo', '123456', @json::jsonb, 'import', 'pending');
        ", new { json });

        var rawRepo = new RawOrderRepository(_conn);
        var studentRepo = new StudentRepository(_conn);
        var enrolRepo = new EnrolmentRepository(_conn);
        var courseRepo = new CourseScheduleRepository(_conn);
        var orderRepo = new OrderRepository(_conn);

        var parser = new WooOrderParser(_log.CreateLogger<WooOrderParser>(), Mock.Of<IFooEventsTicketsClient>());
        var service = new WooOrderService(
            studentRepo,
            enrolRepo,
            courseRepo,
            orderRepo,
            _log.CreateLogger<WooOrderService>()
        );

        var router = new RawOrderRouter(
            parser,
            service,
            Mock.Of<IParser<CanonicalXeroInvoiceDto>>(),
            Mock.Of<IProcessor<CanonicalXeroInvoiceDto>>(),
            Mock.Of<ILogger<RawOrderRouter>>()
        );

        var transformer = new RawOrderTransformer(
            rawRepo,
            router,
            Mock.Of<ILogger<RawOrderTransformer>>(),
            Mock.Of<IXeroApiClient>()
        );

        await transformer.ProcessPendingAsync(CancellationToken.None);

        var enrolments = await _db.QueryAsync<dynamic>(@"
            SELECT e.* 
            FROM bagile.enrolments e
            JOIN bagile.orders o ON o.id = e.order_id
            WHERE o.external_id = '123456';
        ");

        enrolments.Should().HaveCount(1);
    }
}
