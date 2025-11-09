using Bagile.Application.Enrolments.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface IEnrolmentQueries
{
    /// <summary>
    /// Get paginated list of enrolments with filters
    /// </summary>
    Task<IEnumerable<EnrolmentListDto>> GetEnrolmentsAsync(
        long? courseScheduleId,
        long? studentId,
        string? status,
        string? organisation,
        DateTime? from,
        DateTime? to,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Count total enrolments matching filters
    /// </summary>
    Task<int> CountEnrolmentsAsync(
        long? courseScheduleId,
        long? studentId,
        string? status,
        string? organisation,
        DateTime? from,
        DateTime? to,
        CancellationToken ct = default);
}