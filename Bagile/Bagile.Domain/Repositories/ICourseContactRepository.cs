using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface ICourseContactRepository
{
    Task<IEnumerable<CourseContact>> GetByCourseScheduleAsync(long courseScheduleId, CancellationToken ct = default);
    Task<CourseContact> AddAsync(CourseContact contact, CancellationToken ct = default);
    Task<bool> DeleteAsync(long courseScheduleId, long contactId, CancellationToken ct = default);

    /// <summary>
    /// Updates a contact's mutable fields. Returns the updated entity, or null if not found.
    /// The courseScheduleId is included to prevent cross-course edits.
    /// </summary>
    Task<CourseContact?> UpdateAsync(
        long courseScheduleId,
        long contactId,
        string role,
        string name,
        string email,
        string? phone,
        CancellationToken ct = default);
}
