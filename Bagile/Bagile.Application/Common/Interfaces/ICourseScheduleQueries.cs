using Bagile.Application.CourseSchedules.DTOs;
using Bagile.Application.CourseSchedules.Queries.GetScheduleConflicts;

namespace Bagile.Application.Common.Interfaces;

public interface ICourseScheduleQueries
{
    /// <summary>
    /// Get paginated list of course schedules with filters
    /// </summary>
    Task<IEnumerable<CourseScheduleDto>> GetCourseSchedulesAsync(
        DateTime? from,
        DateTime? to,
        string? courseCode,
        string? trainer,
        string? type,
        string? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Count total course schedules matching filters
    /// </summary>
    Task<int> CountCourseSchedulesAsync(
        DateTime? from,
        DateTime? to,
        string? courseCode,
        string? trainer,
        string? type,
        string? status,
        CancellationToken ct = default);

    /// <summary>
    /// Get detailed information for a single course schedule
    /// </summary>
    Task<CourseScheduleDetailDto?> GetCourseScheduleByIdAsync(
        long scheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Get attendees (students) enrolled in a course schedule
    /// </summary>
    Task<IEnumerable<CourseAttendeeDto>> GetCourseAttendeesAsync(
        long scheduleId,
        CancellationToken ct = default);

    /// <summary>
    /// Get raw course data for monitoring (upcoming courses with enrolment counts)
    /// </summary>
    Task<IEnumerable<CourseMonitoringRawDto>> GetCourseMonitoringDataAsync(
        int daysAhead,
        CancellationToken ct = default);

    Task<IEnumerable<ScheduleConflictDto>> GetScheduleConflictsAsync(
        DateTime startDate,
        DateTime endDate,
        string? trainerName,
        CancellationToken ct = default);
}