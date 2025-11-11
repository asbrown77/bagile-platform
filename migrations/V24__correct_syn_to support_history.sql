-- Drop the unique constraint
ALTER TABLE bagile.sync_metadata 
DROP CONSTRAINT IF EXISTS unique_source_entity;

-- Add a new index for queries
CREATE INDEX IF NOT EXISTS idx_sync_metadata_lookup 
ON bagile.sync_metadata(source, entity_type, sync_status, last_synced_at DESC);