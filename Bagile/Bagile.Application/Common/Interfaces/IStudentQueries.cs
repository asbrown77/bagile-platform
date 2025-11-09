using Bagile.Application.Students.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface IStudentQueries
{
    /// <summary>
    /// Get paginated list of students with filters
    /// </summary>
    Task<IEnumerable<StudentDto>> GetStudentsAsync(
        string? email,
        string? name,
        string? organisation,
        string? courseCode,
        int page,
        int pageSize,
        CancellationToken ct = default);

    /// <summary>
    /// Count total students matching filters
    /// </summary>
    Task<int> CountStudentsAsync(
        string? email,
        string? name,
        string? organisation,
        string? courseCode,
        CancellationToken ct = default);

    /// <summary>
    /// Get detailed information for a single student
    /// </summary>
    Task<StudentDetailDto?> GetStudentByIdAsync(
        long studentId,
        CancellationToken ct = default);

    /// <summary>
    /// Get enrolment history for a student
    /// </summary>
    Task<IEnumerable<StudentEnrolmentDto>> GetStudentEnrolmentsAsync(
        long studentId,
        CancellationToken ct = default);
}