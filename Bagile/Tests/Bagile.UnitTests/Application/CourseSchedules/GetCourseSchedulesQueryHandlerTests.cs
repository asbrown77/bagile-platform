using Bagile.Application.Common.Interfaces;
using Bagile.Application.Common.Models;
using Bagile.Application.CourseSchedules.DTOs;
using Bagile.Application.CourseSchedules.Queries.GetCourseSchedules;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Bagile.UnitTests.Application.CourseSchedules;

[TestFixture]
public class GetCourseSchedulesQueryHandlerTests
{
    private Mock<ICourseScheduleQueries> _mockQueries;
    private Mock<ILogger<GetCourseSchedulesQueryHandler>> _mockLogger;
    private GetCourseSchedulesQueryHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _mockQueries = new Mock<ICourseScheduleQueries>();
        _mockLogger = new Mock<ILogger<GetCourseSchedulesQueryHandler>>();
        _handler = new GetCourseSchedulesQueryHandler(_mockQueries.Object, _mockLogger.Object);
    }

    [Test]
    public async Task Handle_Should_Return_Paged_Results()
    {
        // Arrange
        var schedules = new List<CourseScheduleDto>
        {
            new() { Id = 1, CourseCode = "PSM", Title = "PSM Course" },
            new() { Id = 2, CourseCode = "PSPO", Title = "PSPO Course" }
        };

        _mockQueries.Setup(q => q.GetCourseSchedulesAsync(
            null, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);

        _mockQueries.Setup(q => q.CountCourseSchedulesAsync(
            null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var query = new GetCourseSchedulesQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.TotalPages.Should().Be(1);
        result.HasNextPage.Should().BeFalse();
    }

    [Test]
    public async Task Handle_Should_Calculate_Pagination_Correctly()
    {
        // Arrange
        var schedules = Enumerable.Range(1, 20)
            .Select(i => new CourseScheduleDto { Id = i, CourseCode = $"COURSE{i}", Title = $"Course {i}" })
            .ToList();

        _mockQueries.Setup(q => q.GetCourseSchedulesAsync(
            null, null, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedules);

        _mockQueries.Setup(q => q.CountCourseSchedulesAsync(
            null, null, null, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(45); // 45 total records

        var query = new GetCourseSchedulesQuery { Page = 1, PageSize = 20 };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalPages.Should().Be(3); // Ceiling(45 / 20) = 3
        result.HasNextPage.Should().BeTrue();
        result.HasPreviousPage.Should().BeFalse();
    }

    [Test]
    public async Task Handle_Should_Pass_Filters_To_Queries()
    {
        // Arrange
        var query = new GetCourseSchedulesQuery
        {
            CourseCode = "PSM",
            Trainer = "Alex Brown",
            Status = "published",
            From = new DateTime(2025, 1, 1),
            To = new DateTime(2025, 12, 31)
        };

        _mockQueries.Setup(q => q.GetCourseSchedulesAsync(
            query.From, query.To, query.CourseCode, query.Trainer, null, query.Status,
            1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CourseScheduleDto>());

        _mockQueries.Setup(q => q.CountCourseSchedulesAsync(
            query.From, query.To, query.CourseCode, query.Trainer, null, query.Status,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockQueries.Verify(q => q.GetCourseSchedulesAsync(
            query.From, query.To, query.CourseCode, query.Trainer, null, query.Status,
            1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }
}