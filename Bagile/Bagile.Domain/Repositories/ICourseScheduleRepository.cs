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
        Task UpdateStatusAsync(long scheduleId, string status);
        Task<long> InsertPrivateCourseAsync(CourseSchedule schedule);
        Task<bool> ExistsBySkuAsync(string sku);
        Task UpdatePrivateCourseAsync(long id, UpdatePrivateCourseFields fields);
    }
}

/// <summary>Value object carrying the mutable fields for a private course update.</summary>
public record UpdatePrivateCourseFields(
    string Name,
    string? TrainerName,
    DateTime StartDate,
    DateTime EndDate,
    int? Capacity,
    decimal? Price,
    string? InvoiceReference,
    string? VenueAddress,
    string? MeetingUrl,
    string? MeetingId,
    string? MeetingPasscode,
    string? Notes
);
