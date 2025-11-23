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
}
