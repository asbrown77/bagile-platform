-- Migration: Add product sync tracking
-- This allows the system to do incremental syncs instead of fetching all products every time

-- Option 1: Add last_product_sync to track individual product updates
ALTER TABLE bagile.course_schedules 
ADD COLUMN IF NOT EXISTS last_product_sync TIMESTAMP DEFAULT NOW();

-- Option 2: Create a dedicated sync metadata table for tracking sync operations
CREATE TABLE IF NOT EXISTS bagile.sync_metadata (
    id SERIAL PRIMARY KEY,
    source TEXT NOT NULL,           -- e.g., 'WooCommerce'
    entity_type TEXT NOT NULL,       -- e.g., 'products', 'orders'
    last_synced_at TIMESTAMP NOT NULL DEFAULT NOW(),
    records_synced INT DEFAULT 0,
    sync_status TEXT DEFAULT 'success', -- 'success', 'failed', 'in_progress'
    error_message TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT unique_source_entity UNIQUE (source, entity_type)
);

-- Index for quick lookups
CREATE INDEX IF NOT EXISTS idx_sync_metadata_source_entity 
ON bagile.sync_metadata(source, entity_type);

