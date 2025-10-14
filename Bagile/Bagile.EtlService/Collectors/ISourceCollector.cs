namespace Bagile.EtlService.Collectors
{
    public interface ISourceCollector
    {
        string SourceName { get; }
        Task<IEnumerable<string>> CollectOrdersAsync(DateTime? modifiedSince = null, CancellationToken ct = default);
    }

}
