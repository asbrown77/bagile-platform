namespace Bagile.EtlService.Collectors;

public interface IProductCollector
{
    string SourceName { get; }

    /// <summary>
    /// Collects product-type data from the source system
    /// and projects it into the database (e.g. course_schedules table).
    /// </summary>
    Task CollectProductsAsync(CancellationToken ct = default);
}