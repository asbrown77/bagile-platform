using Bagile.Application.Common.Interfaces;
using Bagile.Application.Orders.DTOs;
using Bagile.Application.Orders.Queries;
using Bagile.Application.Orders.Queries.GetOrderById;
using Bagile.Application.Orders.Queries.GetOrders;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Bagile.UnitTests.Application.Orders;

[TestFixture]
public class GetOrdersQueryHandlerTests
{
    private Mock<IOrderQueries> _mockQueries;
    private GetOrdersQueryHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockQueries = new Mock<IOrderQueries>();
        _handler = new GetOrdersQueryHandler(
            _mockQueries.Object,
            NullLogger<GetOrdersQueryHandler>.Instance);
    }

    [Test]
    public async Task Handle_Should_Return_Paged_Orders()
    {
        // Arrange
        var orders = new List<OrderDto>
        {
            new() { Id = 1, ExternalId = "12243", Status = "completed", TotalAmount = 2520 },
            new() { Id = 2, ExternalId = "12244", Status = "processing", TotalAmount = 1050 }
        };

        _mockQueries.Setup(q => q.GetOrdersAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _mockQueries.Setup(q => q.CountOrdersAsync(
            It.IsAny<string>(),
            It.IsAny<DateTime?>(),
            It.IsAny<DateTime?>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var query = new GetOrdersQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Test]
    public async Task Handle_Should_Calculate_Pagination_Correctly()
    {
        // Arrange
        var orders = Enumerable.Range(1, 20).Select(i => new OrderDto { Id = i }).ToList();

        _mockQueries.Setup(q => q.GetOrdersAsync(
            It.IsAny<string>(), null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _mockQueries.Setup(q => q.CountOrdersAsync(
            It.IsAny<string>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(45); // 45 total records

        var query = new GetOrdersQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalPages.Should().Be(3); // Ceiling(45 / 20) = 3
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Test]
    public async Task Handle_Should_Filter_By_Status()
    {
        // Arrange
        var completedOrders = new List<OrderDto>
        {
            new() { Id = 1, Status = "completed" }
        };

        _mockQueries.Setup(q => q.GetOrdersAsync(
            "completed", null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(completedOrders);

        _mockQueries.Setup(q => q.CountOrdersAsync(
            "completed", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetOrdersQuery { Status = "completed", Page = 1, PageSize = 20 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.First().Status.Should().Be("completed");

        _mockQueries.Verify(q => q.GetOrdersAsync(
            "completed", null, null, null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_Should_Filter_By_Date_Range()
    {
        // Arrange
        var from = new DateTime(2024, 1, 1);
        var to = new DateTime(2024, 12, 31);

        _mockQueries.Setup(q => q.GetOrdersAsync(
            null, from, to, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrderDto>());

        _mockQueries.Setup(q => q.CountOrdersAsync(
            null, from, to, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOrdersQuery { From = from, To = to, Page = 1, PageSize = 20 };

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockQueries.Verify(q => q.GetOrdersAsync(
            null, from, to, null, 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_Should_Return_Empty_When_No_Orders()
    {
        // Arrange
        _mockQueries.Setup(q => q.GetOrdersAsync(
            It.IsAny<string>(), null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Empty<OrderDto>());

        _mockQueries.Setup(q => q.CountOrdersAsync(
            It.IsAny<string>(), null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetOrdersQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}

// Tests/Bagile.UnitTests/Application/Orders/GetOrderByIdQueryHandlerTests.cs
[TestFixture]
public class GetOrderByIdQueryHandlerTests
{
    private Mock<IOrderQueries> _mockQueries;
    private GetOrderByIdQueryHandler _handler;

    [SetUp]
    public void Setup()
    {
        _mockQueries = new Mock<IOrderQueries>();
        _handler = new GetOrderByIdQueryHandler(
            _mockQueries.Object,
            NullLogger<GetOrderByIdQueryHandler>.Instance);
    }

    [Test]
    public async Task Handle_Should_Return_Order_When_Found()
    {
        // Arrange
        var expectedOrder = new OrderDetailDto
        {
            Id = 12243,
            ExternalId = "12243",
            Status = "completed",
            TotalAmount = 2520,
            Customer = new CustomerInfo
            {
                Name = "Henry Heselden",
                Email = "henry.heselden@themdu.com",
                Company = "MDU Services Ltd"
            },
            Enrolments = new List<EnrolmentDto>
            {
                new() { StudentEmail = "khalil.nazir@themdu.com", CourseName = "PSM Advanced" }
            }
        };

        _mockQueries.Setup(q => q.GetOrderByIdAsync(12243, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOrder);

        var query = new GetOrderByIdQuery(12243);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(12243);
        result.Customer.Email.Should().Be("henry.heselden@themdu.com");
        result.Enrolments.Should().HaveCount(1);
    }

    [Test]
    public async Task Handle_Should_Return_Null_When_Order_Not_Found()
    {
        // Arrange
        _mockQueries.Setup(q => q.GetOrderByIdAsync(99999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OrderDetailDto?)null);

        var query = new GetOrderByIdQuery(99999);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}