using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

public interface IPreCourseTemplateRepository
{
    Task<IEnumerable<PreCourseTemplate>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Get a template by course type and format (e.g. "PSM", "f2f").
    /// Returns null if no template exists for this combination.
    /// </summary>
    Task<PreCourseTemplate?> GetAsync(string courseType, string format, CancellationToken ct = default);

    /// <summary>
    /// Upsert by (course_type, format). Returns the saved template.
    /// </summary>
    Task<PreCourseTemplate> UpsertAsync(PreCourseTemplate template, CancellationToken ct = default);
}
