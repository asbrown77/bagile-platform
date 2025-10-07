ALTER TABLE bagile.raw_orders
ADD COLUMN IF NOT EXISTS payload_hash text;
