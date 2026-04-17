using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IPlannedCourseRepository
{
    Task<int> InsertAsync(PlannedCourse course);
    Task<PlannedCourse?> GetByIdAsync(int id);
    Task<IEnumerable<PlannedCourse>> GetAllAsync(CancellationToken ct = default);
    Task UpdateAsync(int id, PlannedCourse course);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasPublicationsAsync(int id);
}
