using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories
{
    public interface ICourseScheduleRepository
    {
        Task UpsertAsync(CourseSchedule schedule);
        Task<long?> GetIdBySourceProductAsync(string sourceSystem, long sourceProductId);
        Task<IEnumerable<CourseSchedule>> GetAllAsync();
    }
}
