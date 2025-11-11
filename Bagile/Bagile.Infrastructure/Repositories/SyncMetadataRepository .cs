using Bagile.Domain.Entities;
using Bagile.Domain.Repositories;
using Dapper;
using Npgsql;

public class SyncMetadataRepository : ISyncMetadataRepository
{
    private readonly string _connectionString;

    public SyncMetadataRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<DateTime?> GetLastSuccessfulSyncTimeAsync(string source, string entityType)
    {
        // Get the MOST RECENT successful sync
        const string sql = @"
            SELECT last_synced_at
            FROM bagile.sync_metadata
            WHERE source = @Source
              AND entity_type = @EntityType
              AND sync_status = 'success'
            ORDER BY last_synced_at DESC
            LIMIT 1;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<DateTime?>(sql, new { Source = source, EntityType = entityType });
    }

    public async Task RecordSyncSuccessAsync(string source, string entityType, int recordsSynced)
    {
        // ALWAYS INSERT a new row (no ON CONFLICT)
        const string sql = @"
            INSERT INTO bagile.sync_metadata (source, entity_type, last_synced_at, sync_status, records_synced, created_at)
            VALUES (@Source, @EntityType, @Now, 'success', @RecordsSynced, @Now);";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new
        {
            Source = source,
            EntityType = entityType,
            Now = DateTime.UtcNow,
            RecordsSynced = recordsSynced
        });
    }

    public async Task RecordSyncFailureAsync(string source, string entityType, string errorMessage)
    {
        // ALWAYS INSERT a new row (no ON CONFLICT)
        const string sql = @"
            INSERT INTO bagile.sync_metadata (source, entity_type, last_synced_at, sync_status, error_message, records_synced, created_at)
            VALUES (@Source, @EntityType, @Now, 'failed', @ErrorMessage, 0, @Now);";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new
        {
            Source = source,
            EntityType = entityType,
            Now = DateTime.UtcNow,
            ErrorMessage = errorMessage
        });
    }

    public async Task RecordSyncStartAsync(string source, string entityType)
    {
        // ALWAYS INSERT a new row
        const string sql = @"
            INSERT INTO bagile.sync_metadata (source, entity_type, last_synced_at, sync_status, records_synced, created_at)
            VALUES (@Source, @EntityType, @Now, 'in_progress', 0, @Now);";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new { Source = source, EntityType = entityType, Now = DateTime.UtcNow });
    }

    public async Task<SyncMetadata?> GetSyncMetadataAsync(string source, string entityType)
    {
        // Get the MOST RECENT sync (regardless of status)
        const string sql = @"
            SELECT 
                id as Id,
                source as Source,
                entity_type as EntityType,
                last_synced_at as LastSyncedAt,
                records_synced as RecordsSynced,
                sync_status as SyncStatus,
                error_message as ErrorMessage,
                created_at as CreatedAt
            FROM bagile.sync_metadata
            WHERE source = @Source
              AND entity_type = @EntityType
            ORDER BY last_synced_at DESC
            LIMIT 1;";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        return await conn.QueryFirstOrDefaultAsync<SyncMetadata>(sql, new { Source = source, EntityType = entityType });
    }

    // Optional: Cleanup old records
    public async Task CleanupOldSyncsAsync(int keepLastNRecords = 100)
    {
        const string sql = @"
            DELETE FROM bagile.sync_metadata
            WHERE id NOT IN (
                SELECT id 
                FROM bagile.sync_metadata 
                ORDER BY last_synced_at DESC 
                LIMIT @KeepCount
            );";

        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();
        await conn.ExecuteAsync(sql, new { KeepCount = keepLastNRecords });
    }
}