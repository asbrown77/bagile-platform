using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using Bagile.Application.CourseSchedules.Queries.GetCourseMonitoring;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Bagile.UnitTests.Application.CourseSchedules;

[TestFixture]
public class GetCourseMonitoringQueryHandlerTests
{
    private Mock<ICourseScheduleQueries> _mockQueries;
    private Mock<ILogger<GetCourseMonitoringQueryHandler>> _mockLogger;
    private GetCourseMonitoringQueryHandler _handler;

    [SetUp]
    public void SetUp()
    {
        _mockQueries = new Mock<ICourseScheduleQueries>();
        _mockLogger = new Mock<ILogger<GetCourseMonitoringQueryHandler>>();
        _handler = new GetCourseMonitoringQueryHandler(_mockQueries.Object, _mockLogger.Object);
    }

    // -------------------------------------------------------------------------
    // ExtractBaseCourseCode — internal static, called directly
    // -------------------------------------------------------------------------

    [TestCase("PSPO-280526-CB",    ExpectedResult = "PSPO")]
    [TestCase("PSM-A-230426-AB",   ExpectedResult = "PSM-A")]
    [TestCase("PSFS-010626-CB",    ExpectedResult = "PSFS")]
    [TestCase("PSPOAI-040626-CB",  ExpectedResult = "PSPOAI")]
    [TestCase("",                  ExpectedResult = "")]
    public string ExtractBaseCourseCode_Should_Return_Correct_Code(string sku)
    {
        return GetCourseMonitoringQueryHandler.ExtractBaseCourseCode(sku);
    }

    /// <summary>
    /// Known limitation: "APS-SD-100926-AB" returns "APS" rather than "APS-SD" because
    /// the parser's trainer-initials heuristic (2 uppercase chars) incorrectly treats "SD"
    /// as a trainer suffix. APS-SD enrolments still get minimum=4 because "APS" is also
    /// in the InteractiveCourses set. See PROGRESS.md for remediation.
    /// </summary>
    [Test]
    public void ExtractBaseCourseCode_APS_SD_Returns_APS_Due_To_Known_Parser_Limitation()
    {
        // Actual behaviour: "SD" is misidentified as trainer initials and the loop breaks early.
        // Desired behaviour would be "APS-SD", but that requires a longer trainer-suffix
        // minimum or an allowlist of multi-segment course codes.
        var result = GetCourseMonitoringQueryHandler.ExtractBaseCourseCode("APS-SD-100926-AB");
        result.Should().Be("APS", because: "SD is misidentified as trainer initials — known parser limitation");
    }

    [Test]
    public void ExtractBaseCourseCode_Whitespace_Only_Returns_Empty()
    {
        GetCourseMonitoringQueryHandler.ExtractBaseCourseCode("   ").Should().Be("");
    }

    // -------------------------------------------------------------------------
    // CalculateDecisionDeadline — internal static, called directly
    // -------------------------------------------------------------------------

    [Test]
    public void CalculateDecisionDeadline_Monday_Returns_Previous_Friday()
    {
        // 2026-03-30 is a Monday
        var monday = new DateTime(2026, 3, 30);
        monday.DayOfWeek.Should().Be(DayOfWeek.Monday);

        var deadline = GetCourseMonitoringQueryHandler.CalculateDecisionDeadline(monday);

        deadline.Should().Be(new DateTime(2026, 3, 27)); // previous Friday
        deadline.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    [Test]
    public void CalculateDecisionDeadline_Tuesday_Returns_Previous_Friday()
    {
        // 2026-03-31 is a Tuesday
        var tuesday = new DateTime(2026, 3, 31);
        tuesday.DayOfWeek.Should().Be(DayOfWeek.Tuesday);

        var deadline = GetCourseMonitoringQueryHandler.CalculateDecisionDeadline(tuesday);

        deadline.Should().Be(new DateTime(2026, 3, 27)); // previous Friday
        deadline.DayOfWeek.Should().Be(DayOfWeek.Friday);
    }

    [Test]
    public void CalculateDecisionDeadline_Wednesday_Returns_Monday()
    {
        // 2026-04-01 is a Wednesday
        var wednesday = new DateTime(2026, 4, 1);
        wednesday.DayOfWeek.Should().Be(DayOfWeek.Wednesday);

        var deadline = GetCourseMonitoringQueryHandler.CalculateDecisionDeadline(wednesday);

        deadline.Should().Be(new DateTime(2026, 3, 30)); // 2 days before = Monday
        deadline.DayOfWeek.Should().Be(DayOfWeek.Monday);
    }

    [Test]
    public void CalculateDecisionDeadline_Thursday_Returns_Tuesday()
    {
        // 2026-04-02 is a Thursday
        var thursday = new DateTime(2026, 4, 2);
        thursday.DayOfWeek.Should().Be(DayOfWeek.Thursday);

        var deadline = GetCourseMonitoringQueryHandler.CalculateDecisionDeadline(thursday);

        deadline.Should().Be(new DateTime(2026, 3, 31)); // 2 days before = Tuesday
        deadline.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
    }

    [Test]
    public void CalculateDecisionDeadline_Friday_Returns_Wednesday()
    {
        // 2026-04-03 is a Friday
        var friday = new DateTime(2026, 4, 3);
        friday.DayOfWeek.Should().Be(DayOfWeek.Friday);

        var deadline = GetCourseMonitoringQueryHandler.CalculateDecisionDeadline(friday);

        deadline.Should().Be(new DateTime(2026, 4, 1)); // 2 days before = Wednesday
        deadline.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
    }

    // -------------------------------------------------------------------------
    // GetMinimum (via handler) — standard vs interactive courses
    // -------------------------------------------------------------------------

    [TestCase("PSM-280526-CB",     3, Description = "PSM → standard minimum 3")]
    [TestCase("PSPO-280526-CB",    3, Description = "PSPO → standard minimum 3")]
    [TestCase("PSK-280526-CB",     3, Description = "PSK → standard minimum 3")]
    [TestCase("PSM-A-230426-AB",   4, Description = "PSM-A → interactive minimum 4")]
    [TestCase("PSFS-010626-CB",    4, Description = "PSFS → interactive minimum 4")]
    [TestCase("APS-100926-AB",     4, Description = "APS → interactive minimum 4")]
    [TestCase("APS-SD-100926-AB",  4, Description = "APS-SD → interactive minimum 4")]
    public async Task Handle_MinimumRequired_Reflects_Course_Type(string sku, int expectedMinimum)
    {
        var today = DateTime.UtcNow.Date;
        var startDate = today.AddDays(14); // well in the future, deadline not passed

        _mockQueries
            .Setup(q => q.GetCourseMonitoringDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new CourseMonitoringRawDto
                {
                    Id = 1,
                    CourseCode = sku,
                    Title = "Test Course",
                    StartDate = startDate,
                    CurrentEnrolmentCount = expectedMinimum, // at minimum to keep status simple
                    Status = "publish"
                }
            });

        var result = (await _handler.Handle(new GetCourseMonitoringQuery(), CancellationToken.None)).ToList();

        result.Should().HaveCount(1);
        result[0].MinimumRequired.Should().Be(expectedMinimum);
    }

    // -------------------------------------------------------------------------
    // MonitoringStatus — via handler
    // We fix StartDate relative to a real future date so deadline maths are stable.
    // -------------------------------------------------------------------------

    [Test]
    public async Task Handle_Status_Is_Cancelled_When_Course_Status_Is_Cancelled()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(14);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Cancelled Course",
            StartDate = startDate, CurrentEnrolmentCount = 5, Status = "cancelled"
        });

        var result = await RunHandler();

        result[0].MonitoringStatus.Should().Be("cancelled");
    }

    [Test]
    public async Task Handle_Status_Is_Cancelled_When_Course_Status_Is_Sold_Out()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(14);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Sold Out Course",
            StartDate = startDate, CurrentEnrolmentCount = 3, Status = "sold_out"
        });

        var result = await RunHandler();

        result[0].MonitoringStatus.Should().Be("cancelled");
    }

    [Test]
    public async Task Handle_Status_Is_Healthy_When_Enrolments_At_Minimum()
    {
        // Start date well in the future so deadline is not passed
        var startDate = DateTime.UtcNow.Date.AddDays(21);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Healthy Course",
            StartDate = startDate, CurrentEnrolmentCount = 3, Status = "publish"
        });

        var result = await RunHandler();

        result[0].MonitoringStatus.Should().Be("healthy");
    }

    [Test]
    public async Task Handle_Status_Is_Healthy_When_Enrolments_Exceed_Minimum()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(21);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Over-subscribed Course",
            StartDate = startDate, CurrentEnrolmentCount = 8, Status = "publish"
        });

        var result = await RunHandler();

        result[0].MonitoringStatus.Should().Be("healthy");
    }

    [Test]
    public async Task Handle_Status_Is_Critical_When_Deadline_Passed_And_Below_Minimum()
    {
        // Start date in the past so the deadline has definitely passed
        var startDate = DateTime.UtcNow.Date.AddDays(-1);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Past Course",
            StartDate = startDate, CurrentEnrolmentCount = 1, Status = "publish"
        });

        var result = await RunHandler();

        result[0].MonitoringStatus.Should().Be("critical");
    }

    [Test]
    public async Task Handle_Status_Is_At_Risk_When_Zero_Enrolments_And_Deadline_Not_Passed()
    {
        // Start date far enough ahead that deadline is still in future (> 3 days)
        var startDate = DateTime.UtcNow.Date.AddDays(21);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Empty Course",
            StartDate = startDate, CurrentEnrolmentCount = 0, Status = "publish"
        });

        var result = await RunHandler();

        result[0].MonitoringStatus.Should().Be("at_risk");
    }

    // -------------------------------------------------------------------------
    // RecommendedAction — key scenarios
    // -------------------------------------------------------------------------

    [Test]
    public async Task Handle_RecommendedAction_Contains_Cancel_When_Zero_Enrolments_And_Deadline_Passed()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-1); // past, deadline definitely passed

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Zero enrolments, overdue",
            StartDate = startDate, CurrentEnrolmentCount = 0, Status = "publish"
        });

        var result = await RunHandler();

        result[0].RecommendedAction.Should().Contain("Cancel");
    }

    [Test]
    public async Task Handle_RecommendedAction_Is_Good_To_Go_When_At_Minimum()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(21);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Ready course",
            StartDate = startDate, CurrentEnrolmentCount = 3, Status = "publish"
        });

        var result = await RunHandler();

        result[0].RecommendedAction.Should().Be("Good to go");
    }

    [Test]
    public async Task Handle_RecommendedAction_Is_Already_Cancelled_When_Status_Is_Cancelled()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(14);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Cancelled course",
            StartDate = startDate, CurrentEnrolmentCount = 0, Status = "cancelled"
        });

        var result = await RunHandler();

        result[0].RecommendedAction.Should().Be("Already cancelled");
    }

    [Test]
    public async Task Handle_RecommendedAction_Includes_Enrolment_Counts_When_Below_Minimum_And_Deadline_Passed()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(-1); // past

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Underenrolled",
            StartDate = startDate, CurrentEnrolmentCount = 2, Status = "publish"
        });

        var result = await RunHandler();

        // Expects: "Cancel or contact — 2/3, deadline passed"
        result[0].RecommendedAction.Should().Contain("2/3");
        result[0].RecommendedAction.Should().Contain("deadline passed");
    }

    // -------------------------------------------------------------------------
    // Handler plumbing — DaysAhead forwarded, output fields mapped correctly
    // -------------------------------------------------------------------------

    [Test]
    public async Task Handle_Passes_DaysAhead_To_Query()
    {
        _mockQueries
            .Setup(q => q.GetCourseMonitoringDataAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CourseMonitoringRawDto>());

        await _handler.Handle(new GetCourseMonitoringQuery { DaysAhead = 60 }, CancellationToken.None);

        _mockQueries.Verify(
            q => q.GetCourseMonitoringDataAsync(60, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Handle_Returns_Empty_When_No_Courses()
    {
        _mockQueries
            .Setup(q => q.GetCourseMonitoringDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CourseMonitoringRawDto>());

        var result = await _handler.Handle(new GetCourseMonitoringQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Test]
    public async Task Handle_Maps_Raw_Fields_To_Dto()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(21);
        var endDate = startDate.AddDays(1);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 42,
            CourseCode = "PSM-280526-CB",
            Title = "Professional Scrum Master",
            StartDate = startDate,
            EndDate = endDate,
            TrainerName = "Alex Brown",
            Location = "Online",
            CurrentEnrolmentCount = 3,
            Status = "publish"
        });

        var result = await RunHandler();

        result.Should().HaveCount(1);
        var dto = result[0];
        dto.Id.Should().Be(42);
        dto.CourseCode.Should().Be("PSM-280526-CB");
        dto.Title.Should().Be("Professional Scrum Master");
        dto.StartDate.Should().Be(startDate);
        dto.EndDate.Should().Be(endDate);
        dto.TrainerName.Should().Be("Alex Brown");
        dto.Location.Should().Be("Online");
        dto.CurrentEnrolmentCount.Should().Be(3);
    }

    [Test]
    public async Task Handle_Calculates_FillPercentage_Correctly()
    {
        var startDate = DateTime.UtcNow.Date.AddDays(21);

        SetupSingleCourse(new CourseMonitoringRawDto
        {
            Id = 1, CourseCode = "PSM-280526-CB", Title = "Test",
            StartDate = startDate, CurrentEnrolmentCount = 2, Status = "publish"
        });

        var result = await RunHandler();

        // 2 / 3 minimum = 67% (rounded)
        result[0].FillPercentage.Should().Be(67);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private void SetupSingleCourse(CourseMonitoringRawDto raw)
    {
        _mockQueries
            .Setup(q => q.GetCourseMonitoringDataAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { raw });
    }

    private async Task<List<CourseMonitoringDto>> RunHandler()
    {
        var result = await _handler.Handle(new GetCourseMonitoringQuery(), CancellationToken.None);
        return result.ToList();
    }
}
