namespace Bagile.Domain.Entities;

/// <summary>
/// Tracks ETL sync operations for monitoring and incremental syncing
/// </summary>
public class SyncMetadata
{
    public int Id { get; set; }

    /// <summary>
    /// Source system name (e.g., 'WooCommerce', 'Xero')
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// Type of entity being synced (e.g., 'products', 'orders', 'invoices')
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp of the last successful sync
    /// </summary>
    public DateTime LastSyncedAt { get; set; }

    /// <summary>
    /// Number of records synced in the last operation
    /// </summary>
    public int RecordsSynced { get; set; }

    /// <summary>
    /// Status of the last sync operation
    /// </summary>
    public string SyncStatus { get; set; } = "success"; // 'success', 'failed', 'in_progress'

    /// <summary>
    /// Error message if sync failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}