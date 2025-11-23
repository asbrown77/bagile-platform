using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Bagile.EtlService.Models;
using Bagile.EtlService.Services;
using Bagile.Infrastructure.Clients;
using Bagile.Infrastructure.Repositories;
using Dapper;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;

namespace Bagile.IntegrationTests.EtlService;

[TestFixture]
public class RawOrderTransformerTests
{
    private string _conn = null!;
    private NpgsqlConnection _db = null!;

    [OneTimeSetUp]
    public async Task Setup()
    {
        _conn = DatabaseFixture.ConnectionString;
        _db = new NpgsqlConnection(_conn);
        await _db.OpenAsync();

        await _db.ExecuteAsync("DELETE FROM bagile.enrolments;");
        await _db.ExecuteAsync("DELETE FROM bagile.students WHERE email LIKE '%test%';");
        await _db.ExecuteAsync("DELETE FROM bagile.orders WHERE external_id LIKE 'TEST-%';");
        await _db.ExecuteAsync("DELETE FROM bagile.raw_orders WHERE external_id LIKE 'TEST-%';");

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.course_schedules
            (name, sku, source_system, source_product_id, start_date, status)
            VALUES ('Test PSPO', 'PSPO-010125-AB', 'woo', 11840, NOW(), 'publish')
            ON CONFLICT DO NOTHING;
        ");
    }

    [OneTimeTearDown]
    public void Teardown()
    {
        _db?.Dispose();
    }

    [Test]
    public async Task EndToEnd_WooOrder_ShouldCreateEnrolment()
    {
        var json = @"{
            ""id"": 20001,
            ""billing"": { ""email"": ""transform@test.com"" },
            ""line_items"": [
                { ""product_id"": 11840, ""sku"": ""PSPO-010125-AB"" }
            ]
        }";

        await _db.ExecuteAsync(@"
            INSERT INTO bagile.raw_orders
            (source, external_id, payload, event_type, status)
            VALUES ('woo', '20001', @json::jsonb, 'import', 'pending')
        ", new { json });

        var rawRepo = new RawOrderRepository(_conn);
        var student = new StudentRepository(_conn);
        var enrol = new EnrolmentRepository(_conn);
        var course = new CourseScheduleRepository(_conn);
        var order = new OrderRepository(_conn);

        var parser = new WooOrderParser(Mock.Of<ILogger<WooOrderParser>>(), Mock.Of<IFooEventsTicketsClient>());
        var svc = new WooOrderService(student, enrol, course, order, Mock.Of<ILogger<WooOrderService>>());

        var router = new RawOrderRouter(
            parser,
            svc,
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

        var count = await _db.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*)
            FROM bagile.enrolments e
            JOIN bagile.orders o ON o.id = e.order_id
            WHERE o.external_id = '20001'
        ");

        Assert.That(count, Is.EqualTo(1));
    }
}
