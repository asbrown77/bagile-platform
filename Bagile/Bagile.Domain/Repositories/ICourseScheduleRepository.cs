using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories
{
    public interface ICourseScheduleRepository
    {
        Task UpsertAsync(CourseSchedule schedule);
        Task<long?> GetIdBySourceProductAsync(long sourceProductId);
        Task<CourseSchedule?> GetBySourceProductIdAsync(long sourceProductId);
        Task<long?> GetIdBySkuAsync(string sku);
        Task<IEnumerable<CourseSchedule>> GetAllAsync();
        Task<long> UpsertFromWooPayloadAsync(long productId, string? courseName, string? sku, DateTime? startDate, DateTime? endDate, decimal? price, string? currency);

    }
}
