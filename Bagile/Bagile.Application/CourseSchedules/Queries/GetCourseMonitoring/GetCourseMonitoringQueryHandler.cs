using Bagile.Application.Common.Interfaces;
using Bagile.Application.CourseSchedules.DTOs;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Bagile.Application.CourseSchedules.Queries.GetCourseMonitoring;

public class GetCourseMonitoringQueryHandler
    : IRequestHandler<GetCourseMonitoringQuery, IEnumerable<CourseMonitoringDto>>
{
    private readonly ICourseScheduleQueries _queries;
    private readonly ILogger<GetCourseMonitoringQueryHandler> _logger;

    // Interactive courses require minimum 4 attendees
    private static readonly HashSet<string> InteractiveCourses = new(StringComparer.OrdinalIgnoreCase)
    {
        "PSM-A", "PSFS", "APS", "APS-SD"
    };

    public GetCourseMonitoringQueryHandler(
        ICourseScheduleQueries queries,
        ILogger<GetCourseMonitoringQueryHandler> logger)
    {
        _queries = queries;
        _logger = logger;
    }

    public async Task<IEnumerable<CourseMonitoringDto>> Handle(
        GetCourseMonitoringQuery request,
        CancellationToken ct)
    {
        _logger.LogInformation("Fetching course monitoring data for next {Days} days", request.DaysAhead);

        var rawCourses = await _queries.GetCourseMonitoringDataAsync(request.DaysAhead, ct);

        var today = DateTime.UtcNow.Date;
        var results = new List<CourseMonitoringDto>();

        foreach (var course in rawCourses)
        {
            var startDate = course.StartDate ?? today.AddDays(999);
            var minimum = GetMinimum(course.CourseCode);
            var daysUntilStart = (int)(startDate - today).TotalDays;
            var deadline = CalculateDecisionDeadline(startDate);
            var daysUntilDecision = (int)(deadline - today).TotalDays;
            var fillPct = minimum > 0 ? Math.Round(course.CurrentEnrolmentCount * 100.0 / minimum, 0) : 100;
            var status = DetermineStatus(course.CurrentEnrolmentCount, minimum, daysUntilDecision, course.Status);
            var action = RecommendAction(course.CurrentEnrolmentCount, minimum, daysUntilDecision, course.CourseCode, status);

            results.Add(new CourseMonitoringDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                Title = course.Title,
                StartDate = course.StartDate,
                EndDate = course.EndDate,
                TrainerName = course.TrainerName,
                Location = course.Location,
                CurrentEnrolmentCount = course.CurrentEnrolmentCount,
                MinimumRequired = minimum,
                FillPercentage = fillPct,
                MonitoringStatus = status,
                DecisionDeadline = deadline,
                DaysUntilStart = daysUntilStart,
                DaysUntilDecision = daysUntilDecision,
                RecommendedAction = action
            });
        }

        return results.OrderBy(r => r.DaysUntilDecision).ThenBy(r => r.FillPercentage);
    }

    private static int GetMinimum(string courseCode)
    {
        var baseCode = ExtractBaseCourseCode(courseCode);
        return InteractiveCourses.Contains(baseCode) ? 4 : 3;
    }

    /// <summary>
    /// Extract base course code from SKU (e.g. "PSM-A-230426-AB" → "PSM-A")
    /// </summary>
    internal static string ExtractBaseCourseCode(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku)) return "";

        // SKU format: COURSECODE-DDMMYY-TRAINER (e.g. PSPO-280526-CB, APS-SD-100926-AB)
        // Strategy: find the date segment (6 digits), everything before it is the course code
        var parts = sku.Split('-');
        var codeParts = new List<string>();

        foreach (var part in parts)
        {
            if (part.Length == 6 && part.All(char.IsDigit))
                break;
            codeParts.Add(part);
        }

        // Remove trailing trainer initials if present (2 chars, all uppercase, after course code)
        // This handles edge cases where there's no date segment
        if (codeParts.Count > 1
            && codeParts[^1].Length == 2
            && codeParts[^1].All(char.IsUpper)
            && parts.Length == codeParts.Count)
        {
            codeParts.RemoveAt(codeParts.Count - 1);
        }

        return string.Join("-", codeParts);
    }

    /// <summary>
    /// Decision deadline: Mon/Tue course → previous Friday. Wed-Fri → 2 business days before.
    /// </summary>
    internal static DateTime CalculateDecisionDeadline(DateTime startDate)
    {
        return startDate.DayOfWeek switch
        {
            DayOfWeek.Monday => startDate.AddDays(-3),    // Previous Friday
            DayOfWeek.Tuesday => startDate.AddDays(-4),   // Previous Friday
            DayOfWeek.Wednesday => startDate.AddDays(-2),  // Monday
            DayOfWeek.Thursday => startDate.AddDays(-2),   // Tuesday
            DayOfWeek.Friday => startDate.AddDays(-2),     // Wednesday
            DayOfWeek.Saturday => startDate.AddDays(-3),   // Wednesday
            DayOfWeek.Sunday => startDate.AddDays(-2),     // Friday
            _ => startDate.AddDays(-2)
        };
    }

    private static string DetermineStatus(int enrolments, int minimum, int daysUntilDecision, string? courseStatus)
    {
        if (courseStatus is "sold_out" or "cancelled")
            return "cancelled";
        if (enrolments >= minimum)
            return "healthy";
        if (daysUntilDecision <= 0)
            return "critical";
        if (daysUntilDecision <= 3 || enrolments == 0)
            return "at_risk";
        return "at_risk";
    }

    private static string RecommendAction(int enrolments, int minimum, int daysUntilDecision, string courseCode, string status)
    {
        if (status == "cancelled")
            return "Already cancelled";
        if (status == "healthy")
            return "Good to go";
        if (enrolments == 0 && daysUntilDecision <= 0)
            return "Cancel — zero enrolments, deadline passed";
        if (enrolments == 0 && daysUntilDecision <= 3)
            return "Cancel — zero enrolments, deadline imminent";
        if (enrolments == 0)
            return "Monitor — zero enrolments, decision needed soon";
        if (enrolments < minimum && daysUntilDecision <= 0)
            return $"Cancel or contact — {enrolments}/{minimum}, deadline passed";
        if (enrolments < minimum && daysUntilDecision <= 3)
            return $"Push for bookings — {enrolments}/{minimum}, {daysUntilDecision} days to decide";
        return $"Monitor — {enrolments}/{minimum}, {daysUntilDecision} days to decide";
    }
}
