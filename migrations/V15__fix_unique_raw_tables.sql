ALTER TABLE bagile.raw_orders
ADD CONSTRAINT uq_raw_orders_source_hash UNIQUE (source, payload_hash);
