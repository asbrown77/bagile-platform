using Bagile.Application.PlannedCourses.DTOs;

namespace Bagile.Application.Common.Interfaces;

public interface IPlannedCourseQueries
{
    Task<PlannedCourseDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IEnumerable<PlannedCourseDto>> GetAllAsync(CancellationToken ct = default);
}
