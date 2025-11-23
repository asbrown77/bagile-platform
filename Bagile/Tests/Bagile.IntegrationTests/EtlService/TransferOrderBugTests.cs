using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Models;
using Bagile.EtlService.Services;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Models;
using Bagile.Infrastructure.Repositories;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Npgsql;
using NUnit.Framework;
using System.Text.Json;

namespace Bagile.IntegrationTests.EtlService;

[TestFixture]
[Category("Integration")]
public class NewWooOrderPipelineTests
{
    private string _conn = null!;
    private NpgsqlConnection _db = null!;

    private readonly ILoggerFactory _loggerFactory =
        LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Debug));

    [OneTimeSetUp]
    public async Task Setup()
    {
        _conn = DatabaseFixture.ConnectionString;
        _db = new NpgsqlConnection(_conn);
        await _db.OpenAsync();

        // clean tables
        await _db.ExecuteAsync("DELETE FROM bagile.enrolments;");
        await _db.ExecuteAsync("DELETE FROM bagile.students WHERE email LIKE '%pipeline-test%';");
        await _db.ExecuteAsync("DELETE FROM bagile.orders WHERE external_id LIKE 'TEST-%';");
        await _db.ExecuteAsync("DELETE FROM bagile.raw_orders WHERE external_id LIKE 'TEST-%';");

        // seed course for tests
        await SeedScheduleIfMissing(
            sourceProductId: 11840,
            sku: "PSPO-010125-AB",
            name: "Test PSPO Course");
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        _db?.Dispose();
        _loggerFactory?.Dispose();
    }


    // ======================================================================
    //  HELPERS
    // ======================================================================

    private RawOrderTransformer CreateTransformer()
    {
        var rawRepo = new RawOrderRepository(_conn);
        var orderRepo = new OrderRepository(_conn);
        var studentRepo = new StudentRepository(_conn);
        var enrolRepo = new EnrolmentRepository(_conn);
        var courseRepo = new CourseScheduleRepository(_conn);

        var mockFoo = new Mock<IFooEventsTicketsClient>();
        mockFoo.Setup(x => x.FetchTicketsForOrderAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<FooEventTicketDto>());

        var parser = new WooOrderParser(
            NullLogger<WooOrderParser>.Instance);

        var service = new WooOrderService(
            studentRepo,
            enrolRepo,
            courseRepo,
            mockFoo.Object,
            NullLogger<WooOrderService>.Instance);

        return new RawOrderTransformer(
            rawRepo,
            parser,
            service,
            NullLogger<RawOrderTransformer>.Instance
        );
    }

    private async Task<long> SeedScheduleIfMissing(long sourceProductId, string sku, string name)
    {
        var existing = await _db.ExecuteScalarAsync<long?>(@"
            SELECT id FROM bagile.course_schedules
            WHERE source_product_id = @pid
        ", new { pid = sourceProductId });

        if (existing.HasValue)
            return existing.Value;

        return await _db.ExecuteScalarAsync<long>(@"
            INSERT INTO bagile.course_schedules
                (name, sku, source_system, source_product_id, start_date, status)
            VALUES
                (@name, @sku, 'woo', @pid, NOW(), 'publish')
            RETURNING id;
        ", new { name, sku, pid = sourceProductId });
    }


    private async Task InsertRawOrder(string externalId, string json)
    {
        await _db.ExecuteAsync(@"
            INSERT INTO bagile.raw_orders
                (source, external_id, payload, event_type, status)
            VALUES
                ('woo', @ext, @json::jsonb, 'import', 'pending')
        ", new { ext = externalId, json });
    }

    private async Task<int> CountEnrolmentsForOrder(string externalId)
    {
        return await _db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM bagile.enrolments e
            JOIN bagile.orders o ON o.id = e.order_id
            WHERE o.external_id = @ext
        ", new { ext = externalId });
    }


    // ======================================================================
    //  TESTS
    // ======================================================================


    [Test]
    public async Task NormalOrder_CreatesEnrolment()
    {
        var json = @"{
            ""id"": 50001,
            ""number"": ""50001"",
            ""status"": ""completed"",
            ""billing"": { ""email"": ""normal@pipeline-test.com"" },
            ""line_items"": [
                { ""sku"": ""PSPO-010125-AB"", ""product_id"": 11840, ""quantity"": 1 }
            ]
        }";

        await InsertRawOrder("TEST-50001", json);

        var transformer = CreateTransformer();
        await transformer.ProcessPendingAsync(CancellationToken.None);

        var count = await CountEnrolmentsForOrder("TEST-50001");
        count.Should().Be(1);
    }


    [Test]
    public async Task TransferOrder_IsInternalTransfer_CreatesEnrolment()
    {
        var json = @"{
            ""id"": 60001,
            ""number"": ""60001"",
            ""billing"": { ""email"": ""transfer@pipeline-test.com"" },
            ""meta_data"": [],
            ""line_items"": [
                { ""product_id"": 11840, ""sku"": ""PSPO-010125-AB"" }
            ]
        }";

        await InsertRawOrder("TEST-60001", json);

        var transformer = CreateTransformer();
        await transformer.ProcessPendingAsync(CancellationToken.None);

        var count = await CountEnrolmentsForOrder("TEST-60001");
        count.Should().Be(1);
    }


    [Test]
    public async Task InvalidSku_DoesNotCreateNewSchedule()
    {
        var json = @"{
            ""id"": 70001,
            ""number"": ""70001"",
            ""billing"": { ""email"": ""invalid@pipeline-test.com"" },
            ""line_items"": [
                { ""product_id"": 88888, ""sku"": ""INVALID-SKU"" }
            ]
        }";

        await InsertRawOrder("TEST-70001", json);

        var transformer = CreateTransformer();
        await transformer.ProcessPendingAsync(CancellationToken.None);

        // schedule id should be NULL
        var enrol = await _db.QueryFirstOrDefaultAsync<dynamic>(@"
            SELECT e.course_schedule_id
            FROM bagile.enrolments e
            JOIN bagile.orders o ON o.id = e.order_id
            WHERE o.external_id = 'TEST-70001'
        ");

        ((long?)enrol.course_schedule_id).Should().BeNull();
    }


    [Test]
    public async Task BillingOnlyOrder_CreatesEnrolment()
    {
        var json = @"{
            ""id"": 80001,
            ""number"": ""80001"",
            ""billing"": {
                ""email"": ""billing-only@pipeline-test.com"",
                ""first_name"": ""John"",
                ""last_name"": ""Bill""
            },
            ""line_items"": [
                { ""product_id"": 11840, ""quantity"": 1 }
            ]
        }";

        await InsertRawOrder("TEST-80001", json);

        var transformer = CreateTransformer();
        await transformer.ProcessPendingAsync(CancellationToken.None);

        var count = await CountEnrolmentsForOrder("TEST-80001");
        count.Should().Be(1);
    }


    [Test]
    public async Task MultipleTickets_CreateMultipleEnrolments()
    {
        var json = @"{
            ""id"": 90001,
            ""number"": ""90001"",
            ""billing"": { ""email"": ""multi@pipeline-test.com"" },
            ""meta_data"": [
                {
                    ""key"": ""WooCommerceEventsOrderTickets"",
                    ""value"": {
                        ""1"": {
                            ""1"": { ""WooCommerceEventsAttendeeEmail"": ""one@pipeline-test.com"" },
                            ""2"": { ""WooCommerceEventsAttendeeEmail"": ""two@pipeline-test.com"" }
                        }
                    }
                }
            ],
            ""line_items"": [
                { ""product_id"": 11840, ""sku"": ""PSPO-010125-AB"", ""quantity"": 2 }
            ]
        }";

        await InsertRawOrder("TEST-90001", json);

        var transformer = CreateTransformer();
        await transformer.ProcessPendingAsync(CancellationToken.None);

        var count = await CountEnrolmentsForOrder("TEST-90001");
        count.Should().Be(2);
    }
}
