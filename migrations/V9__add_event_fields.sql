ALTER TABLE bagile.raw_orders
    ADD COLUMN IF NOT EXISTS received_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ADD COLUMN IF NOT EXISTS event_type TEXT;

-- Drop any old constraint that included payload
ALTER TABLE bagile.raw_orders
    DROP CONSTRAINT IF EXISTS raw_orders_source_external_id_payload_key,
    DROP CONSTRAINT IF EXISTS raw_orders_source_external_id_key;

-- Optional index for lookup performance
CREATE INDEX IF NOT EXISTS idx_raw_orders_source_external_id
    ON bagile.raw_orders (source, external_id);

    
