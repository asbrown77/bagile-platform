using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface ICoursePublicationRepository
{
    Task<CoursePublication?> GetByPlannedCourseAndGatewayAsync(int plannedCourseId, string gateway, CancellationToken ct = default);
    Task<CoursePublication?> GetByScheduleAndGatewayAsync(long courseScheduleId, string gateway, CancellationToken ct = default);
    Task<int> InsertAsync(CoursePublication publication, CancellationToken ct = default);
    Task<int> DeleteByPlannedCourseAndGatewayAsync(int plannedCourseId, string gateway, CancellationToken ct = default);
}
