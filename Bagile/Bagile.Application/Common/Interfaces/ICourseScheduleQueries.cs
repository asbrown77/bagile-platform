using Bagile.Application.CourseSchedules.DTOs;

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
}