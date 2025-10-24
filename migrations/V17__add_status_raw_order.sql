ALTER TABLE bagile.raw_orders
ADD COLUMN IF NOT EXISTS status TEXT DEFAULT 'pending';