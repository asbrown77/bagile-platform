using Bagile.Domain.Entities;

namespace Bagile.Domain.Repositories;

/// <summary>
/// Repository for tracking ETL sync operations and their status
/// </summary>
public interface ISyncMetadataRepository
{
    /// <summary>
    /// Gets the last successful sync time for a given source and entity type
    /// </summary>
    Task<DateTime?> GetLastSuccessfulSyncTimeAsync(string source, string entityType);

    /// <summary>
    /// Records the start of a sync operation
    /// </summary>
    Task RecordSyncStartAsync(string source, string entityType);

    /// <summary>
    /// Records a successful sync completion
    /// </summary>
    Task RecordSyncSuccessAsync(string source, string entityType, int recordsSynced);

    /// <summary>
    /// Records a failed sync operation
    /// </summary>
    Task RecordSyncFailureAsync(string source, string entityType, string errorMessage);

    /// <summary>
    /// Gets sync statistics for monitoring
    /// </summary>
    Task<SyncMetadata?> GetSyncMetadataAsync(string source, string entityType);
}