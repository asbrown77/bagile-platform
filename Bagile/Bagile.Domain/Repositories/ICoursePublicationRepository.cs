using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface ICoursePublicationRepository
{
    Task<CoursePublication?> GetByPlannedCourseAndGatewayAsync(int plannedCourseId, string gateway, CancellationToken ct = default);
    Task<int> InsertAsync(CoursePublication publication, CancellationToken ct = default);
}
