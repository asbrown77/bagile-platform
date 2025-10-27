-- Add TotalQuantity to orders if not exists
ALTER TABLE bagile.orders
ADD COLUMN IF NOT EXISTS total_quantity INT,
ADD COLUMN IF NOT EXISTS total_tax NUMERIC(12,2),
ADD COLUMN IF NOT EXISTS sub_total NUMERIC(12,2);

-- Optional: backfill with 0 so NULL comparisons don’t break reports
UPDATE bagile.orders SET total_quantity = 0 WHERE total_quantity IS NULL;

