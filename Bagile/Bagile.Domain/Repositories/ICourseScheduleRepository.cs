using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories
{
    public interface ICourseScheduleRepository
    {
        Task UpsertAsync(CourseSchedule schedule);
        Task<IEnumerable<CourseSchedule>> GetAllAsync();
    }
}
