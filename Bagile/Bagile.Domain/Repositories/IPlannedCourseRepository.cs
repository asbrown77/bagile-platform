using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IPlannedCourseRepository
{
    Task<int> InsertAsync(PlannedCourse course);
    Task<PlannedCourse?> GetByIdAsync(int id);
    Task UpdateAsync(int id, PlannedCourse course);
    Task<bool> DeleteAsync(int id);
    Task<bool> HasPublicationsAsync(int id);
}
