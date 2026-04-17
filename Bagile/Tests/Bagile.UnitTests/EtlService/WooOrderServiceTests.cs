using Bagile.EtlService.Services;
using Bagile.EtlService.Models;
using Bagile.Domain.Repositories;
using Bagile.Domain.Entities;
using Bagile.Infrastructure.Clients;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Bagile.UnitTests.EtlService;

[TestFixture]
public class WooOrderServiceTests
{
    private Mock<IStudentRepository> _students = null!;
    private Mock<IEnrolmentRepository> _enrolments = null!;
    private Mock<ICourseScheduleRepository> _courses = null!;
    private Mock<IOrderRepository> _order = null!;
    private WooOrderService _service = null!;

    [SetUp]
    public void Setup()
    {
        _students = new Mock<IStudentRepository>();
        _enrolments = new Mock<IEnrolmentRepository>();
        _courses = new Mock<ICourseScheduleRepository>();
        _order = new Mock<IOrderRepository>();

        _service = new WooOrderService(
            _students.Object,
            _enrolments.Object,
            _courses.Object,
            _order.Object,
            Mock.Of<ILogger<WooOrderService>>()
        );
    }

    [Test]
    public async Task ProcessAsync_CreatesBillingOnlyEnrolment()
    {
        var dto = new CanonicalWooOrderDto
        {
            BillingEmail = "test@billing.com",
            BillingName = "Bill User",
            RawPayload = @"{
                ""billing"": { ""email"": ""test@billing.com"" },
                ""line_items"": [
                    { ""product_id"": 11840, ""sku"": ""PSPO-010125-AB"" }
                ]
            }"
        };

        _order.Setup(x => x.UpsertOrderAsync(It.IsAny<Order>())).ReturnsAsync(101);
        _students.Setup(x => x.UpsertAsync(It.IsAny<Student>()))
            .ReturnsAsync(123);

        _courses.Setup(x => x.GetBySourceProductIdAsync(11840))
            .ReturnsAsync(new CourseSchedule { Id = 77, Sku = "PSPO-010125-AB" });

        await _service.ProcessAsync(dto, CancellationToken.None);

        _students.Verify(x => x.UpsertAsync(It.IsAny<Student>()), Times.Once);
        _enrolments.Verify(x =>
            x.UpsertAsync(It.Is<Enrolment>(e =>
                e.StudentId == 123 &&
                e.OrderId == 101 &&
                e.CourseScheduleId == 77
            )),
            Times.Once
        );
    }

    [Test]
    public async Task ProcessAsync_NewOrder_UpsertsCalled()
    {
        // Arrange — OrderId = 0 means new order, should trigger upsert
        var dto = new CanonicalWooOrderDto
        {
            OrderId = 0,
            ExternalId = "12912",
            BillingEmail = "test@example.com",
            BillingName = "Test User",
            Status = "processing",
            RawPayload = "{}"
        };

        _order.Setup(x => x.UpsertOrderAsync(It.IsAny<Order>())).ReturnsAsync(42);
        _students.Setup(x => x.UpsertAsync(It.IsAny<Student>())).ReturnsAsync(1);

        // Act
        await _service.ProcessAsync(dto, CancellationToken.None);

        // Assert — upsert called with initial status
        _order.Verify(x => x.UpsertOrderAsync(It.Is<Order>(o =>
            o.ExternalId == "12912" && o.Status == "processing"
        )), Times.Once);
    }

    [Test]
    public async Task ProcessAsync_ReprocessedOrder_StatusUpdatedViaUpsert()
    {
        // Arrange — OrderId = 0 (parser doesn't look up existing orders)
        // but ExternalId matches an existing order. The ON CONFLICT upsert
        // in OrderRepository will update the status.
        var dto = new CanonicalWooOrderDto
        {
            OrderId = 0,
            ExternalId = "12912",
            BillingEmail = "test@example.com",
            BillingName = "Test User",
            Status = "completed",
            RawPayload = "{}"
        };

        _order.Setup(x => x.UpsertOrderAsync(It.IsAny<Order>())).ReturnsAsync(42);
        _students.Setup(x => x.UpsertAsync(It.IsAny<Student>())).ReturnsAsync(1);

        // Act
        await _service.ProcessAsync(dto, CancellationToken.None);

        // Assert — upsert called with updated status (ON CONFLICT will handle the update)
        _order.Verify(x => x.UpsertOrderAsync(It.Is<Order>(o =>
            o.ExternalId == "12912" && o.Status == "completed"
        )), Times.Once);
    }

    [Test]
    public async Task ProcessAsync_TicketProcessing_CreatesEnrolmentPerTicket()
    {
        var dto = new CanonicalWooOrderDto
        {
            OrderId = 105,
            BillingEmail = "bill@test.com",
            RawPayload = "{}",
            Tickets = new List<CanonicalTicketDto>
            {
                new CanonicalTicketDto
                {
                    Email = "one@test.com",
                    FirstName = "One",
                    LastName = "Test",
                    Sku = "11840"
                },
                new CanonicalTicketDto
                {
                    Email = "two@test.com",
                    FirstName = "Two",
                    LastName = "Test",
                    Sku = "11840"
                }
            }
        };

        _students.Setup(x => x.UpsertAsync(It.IsAny<Student>()))
            .ReturnsAsync(50);

        _courses.Setup(x => x.GetIdBySkuAsync("11840"))
            .ReturnsAsync(200);

        _enrolments.Setup(x => x.GetByOrderIdAsync(105))
            .ReturnsAsync(new List<Enrolment>());
        _enrolments.Setup(x => x.InsertAsync(It.IsAny<Enrolment>()))
            .ReturnsAsync(1L);

        await _service.ProcessAsync(dto, CancellationToken.None);

        _enrolments.Verify(x => x.InsertAsync(It.IsAny<Enrolment>()), Times.Exactly(2));
    }

    [Test]
    public async Task ProcessAsync_DuplicateEmailTickets_CreatesTwoEnrolments()
    {
        // Regression test for the NobleProg bug:
        // two tickets on the same order share one email (partner admin address),
        // both resolve to the same studentId — must still produce two enrolments.
        var dto = new CanonicalWooOrderDto
        {
            OrderId = 200,
            BillingEmail = "admin@partner.com",
            RawPayload = "{}",
            Tickets = new List<CanonicalTicketDto>
            {
                new() { Email = "admin@partner.com", FirstName = "Alice", LastName = "One", Sku = "PSMA-230426-AB" },
                new() { Email = "admin@partner.com", FirstName = "Bob",   LastName = "Two", Sku = "PSMA-230426-AB" }
            }
        };

        var studentIdSeq = 32L;
        _students.Setup(x => x.UpsertAsync(It.Is<Student>(s => s.Email == "admin@partner.com")))
            .ReturnsAsync(32L); // same studentId for both shared-email tickets
        _students.Setup(x => x.UpsertAsync(It.Is<Student>(s => s.Email != "admin@partner.com")))
            .ReturnsAsync(() => ++studentIdSeq); // synthetic student gets a new id

        _courses.Setup(x => x.GetIdBySkuAsync("PSMA-230426-AB"))
            .ReturnsAsync(19);

        _enrolments.Setup(x => x.GetByOrderIdAsync(200))
            .ReturnsAsync(new List<Enrolment>());
        _enrolments.Setup(x => x.InsertAsync(It.IsAny<Enrolment>()))
            .ReturnsAsync(1L);

        await _service.ProcessAsync(dto, CancellationToken.None);

        // First ticket uses studentId=32, second ticket triggers a synthetic student
        _students.Verify(x => x.UpsertAsync(It.Is<Student>(s =>
            s.Email.EndsWith("@woo.partner")
        )), Times.Once);

        // Each ticket gets its own enrolment row
        _enrolments.Verify(x => x.InsertAsync(It.IsAny<Enrolment>()), Times.Exactly(2));
    }

    [Test]
    public async Task ProcessAsync_MultiTicket_CreatesSeparateStudentPerTicket()
    {
        // Arrange — 2 tickets with different attendee emails
        var dto = new CanonicalWooOrderDto
        {
            OrderId = 0,
            ExternalId = "12874",
            BillingEmail = "billing@company.com",
            BillingName = "Billing User",
            BillingCompany = "Test Corp",
            Status = "completed",
            RawPayload = "{}",
            Tickets = new List<CanonicalTicketDto>
            {
                new() { Email = "attendee1@company.com", FirstName = "Alice", LastName = "One", Sku = "PSPO-300326-AB" },
                new() { Email = "attendee2@company.com", FirstName = "Bob", LastName = "Two", Sku = "PSPO-300326-AB" }
            }
        };

        _order.Setup(x => x.UpsertOrderAsync(It.IsAny<Order>())).ReturnsAsync(42);

        var studentIdCounter = 100L;
        _students.Setup(x => x.UpsertAsync(It.IsAny<Student>()))
            .ReturnsAsync(() => ++studentIdCounter);

        _courses.Setup(x => x.GetIdBySkuAsync("PSPO-300326-AB"))
            .ReturnsAsync(77);

        _enrolments.Setup(x => x.GetByOrderIdAsync(42))
            .ReturnsAsync(new List<Enrolment>());
        _enrolments.Setup(x => x.InsertAsync(It.IsAny<Enrolment>()))
            .ReturnsAsync(1L);

        // Act
        await _service.ProcessAsync(dto, CancellationToken.None);

        // Assert — two different students created
        _students.Verify(x => x.UpsertAsync(It.Is<Student>(s =>
            s.Email == "attendee1@company.com")), Times.AtLeastOnce);
        _students.Verify(x => x.UpsertAsync(It.Is<Student>(s =>
            s.Email == "attendee2@company.com")), Times.Once);

        // Assert — two enrolments with different student IDs
        _enrolments.Verify(x => x.InsertAsync(It.IsAny<Enrolment>()), Times.Exactly(2));
    }

    [Test]
    public async Task ProcessAsync_NoAutoTransfer_AlwaysCreatesNormalEnrolment()
    {
        // Arrange — student already has active enrolment on a different PSPO course
        // The ETL should NOT auto-transfer — just create a new enrolment
        // Transfers are now explicit (via dashboard/MCP)
        var dto = new CanonicalWooOrderDto
        {
            OrderId = 0,
            ExternalId = "12912",
            BillingEmail = "maja@test.com",
            BillingName = "Maja Bek",
            Status = "completed",
            RawPayload = "{}",
            Tickets = new List<CanonicalTicketDto>
            {
                new() { Email = "maja@test.com", FirstName = "Maja", LastName = "Bek", Sku = "PSPO-300326-AB" }
            }
        };

        _order.Setup(x => x.UpsertOrderAsync(It.IsAny<Order>())).ReturnsAsync(42);
        _students.Setup(x => x.UpsertAsync(It.IsAny<Student>())).ReturnsAsync(50);
        _courses.Setup(x => x.GetIdBySkuAsync("PSPO-300326-AB")).ReturnsAsync(77);
        _enrolments.Setup(x => x.GetByOrderIdAsync(42))
            .ReturnsAsync(new List<Enrolment>());
        _enrolments.Setup(x => x.InsertAsync(It.IsAny<Enrolment>()))
            .ReturnsAsync(1L);

        // Act
        await _service.ProcessAsync(dto, CancellationToken.None);

        // Assert — normal enrolment created, no transfer
        _enrolments.Verify(x => x.InsertAsync(It.IsAny<Enrolment>()), Times.Once);
        _enrolments.Verify(x => x.MarkTransferredAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
        _enrolments.Verify(x => x.FindHeuristicTransferSourceAsync(It.IsAny<long>(), It.IsAny<string>()), Times.Never);
    }
}
