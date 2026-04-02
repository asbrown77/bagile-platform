using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface ICourseContactRepository
{
    Task<IEnumerable<CourseContact>> GetByCourseScheduleAsync(long courseScheduleId, CancellationToken ct = default);
    Task<CourseContact> AddAsync(CourseContact contact, CancellationToken ct = default);
    Task<bool> DeleteAsync(long courseScheduleId, long contactId, CancellationToken ct = default);
}
