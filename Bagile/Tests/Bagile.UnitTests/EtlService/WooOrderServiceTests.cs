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

        await _service.ProcessAsync(dto, CancellationToken.None);

        _enrolments.Verify(x => x.UpsertAsync(It.IsAny<Enrolment>()), Times.Exactly(2));
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

        // Act
        await _service.ProcessAsync(dto, CancellationToken.None);

        // Assert — two different students created
        _students.Verify(x => x.UpsertAsync(It.Is<Student>(s =>
            s.Email == "attendee1@company.com")), Times.AtLeastOnce);
        _students.Verify(x => x.UpsertAsync(It.Is<Student>(s =>
            s.Email == "attendee2@company.com")), Times.Once);

        // Assert — two enrolments with different student IDs
        _enrolments.Verify(x => x.UpsertAsync(It.IsAny<Enrolment>()), Times.Exactly(2));
    }

    [Test]
    public async Task ProcessAsync_SameCourseNewBooking_DoesNotTriggerTransfer()
    {
        // Arrange — student already has active enrolment on PSPO course schedule 77
        // New order for the SAME course schedule 77 (not a transfer)
        var dto = new CanonicalWooOrderDto
        {
            OrderId = 0,
            ExternalId = "12890",
            BillingEmail = "mateja@test.com",
            BillingName = "Mateja Macuh",
            Status = "completed",
            RawPayload = "{}",
            Tickets = new List<CanonicalTicketDto>
            {
                new() { Email = "mateja@test.com", FirstName = "Mateja", LastName = "Macuh", Sku = "PSPO-300326-AB" }
            }
        };

        _order.Setup(x => x.UpsertOrderAsync(It.IsAny<Order>())).ReturnsAsync(42);
        _students.Setup(x => x.UpsertAsync(It.IsAny<Student>())).ReturnsAsync(50);
        _courses.Setup(x => x.GetIdBySkuAsync("PSPO-300326-AB")).ReturnsAsync(77);

        // Student already has an active enrolment on this SAME course (schedule 77)
        _enrolments.Setup(x => x.FindHeuristicTransferSourceAsync(50, "PSPO"))
            .ReturnsAsync(new Enrolment { Id = 999, CourseScheduleId = 77, Status = "active" });

        // Act
        await _service.ProcessAsync(dto, CancellationToken.None);

        // Assert — should create normal enrolment, NOT mark as transfer
        _enrolments.Verify(x => x.UpsertAsync(It.IsAny<Enrolment>()), Times.Once);
        _enrolments.Verify(x => x.MarkTransferredAsync(It.IsAny<long>(), It.IsAny<long>()), Times.Never);
    }

    [Test]
    public async Task ProcessAsync_DifferentCourseDate_TriggersTransfer()
    {
        // Arrange — student has active enrolment on OLD PSPO course (schedule 50)
        // New order for DIFFERENT PSPO course (schedule 77) — this IS a transfer
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

        // Student has active enrolment on DIFFERENT course schedule (50, not 77)
        _enrolments.Setup(x => x.FindHeuristicTransferSourceAsync(50, "PSPO"))
            .ReturnsAsync(new Enrolment { Id = 999, CourseScheduleId = 50, Status = "active" });

        // Existing enrolment check
        _enrolments.Setup(x => x.FindAsync(50, 42, 77))
            .ReturnsAsync((Enrolment?)null);

        _enrolments.Setup(x => x.InsertAsync(It.IsAny<Enrolment>()))
            .ReturnsAsync(1000);

        // Act
        await _service.ProcessAsync(dto, CancellationToken.None);

        // Assert — should trigger transfer
        _enrolments.Verify(x => x.MarkTransferredAsync(999, 1000), Times.Once);
    }
}
