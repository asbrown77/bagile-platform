using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IPostCourseTemplateRepository
{
    Task<IEnumerable<PostCourseTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<PostCourseTemplate?> GetByCourseTypeAsync(string courseType, CancellationToken ct = default);

    /// <summary>
    /// Upsert by course_type. Returns the saved template.
    /// </summary>
    Task<PostCourseTemplate> UpsertAsync(PostCourseTemplate template, CancellationToken ct = default);
}
