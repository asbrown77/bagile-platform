ALTER TABLE bagile.raw_orders
ADD COLUMN IF NOT EXISTS processed_at timestamptz NULL;

ALTER TABLE bagile.raw_orders
  RENAME COLUMN imported_at TO created_at;

ALTER TABLE bagile.raw_orders
  DROP COLUMN IF EXISTS received_at;

-- Drop old version
DROP TABLE IF EXISTS bagile.orders CASCADE;

-- Recreate unified version
CREATE TABLE bagile.orders (
    id BIGSERIAL PRIMARY KEY,
    raw_order_id BIGINT REFERENCES bagile.raw_orders(id) ON DELETE SET NULL,
    external_id TEXT UNIQUE,
    source TEXT NOT NULL,             -- 'woo' or 'xero'
    type TEXT NOT NULL,               -- 'public' or 'private'
    reference TEXT,
    billing_company TEXT,
    contact_name TEXT,
    contact_email TEXT,
    total_amount NUMERIC(12,2),
    status TEXT,
    order_date TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Optional but good to have
CREATE INDEX IF NOT EXISTS idx_orders_source ON bagile.orders (source);
CREATE INDEX IF NOT EXISTS idx_orders_external_id ON bagile.orders (external_id);
CREATE INDEX IF NOT EXISTS idx_orders_reference  ON bagile.orders (reference);
