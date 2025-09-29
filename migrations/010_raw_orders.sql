CREATE TABLE bagile.raw_orders (
	id SERIAL PRIMARY KEY,
	source TEXT NOT NULL,              -- e.g. "woo", "xero"
	external_id TEXT NOT NULL,         -- external system's unique ID (Woo order id, Xero invoice id, etc.)
	payload JSONB NOT NULL,            -- raw JSON from the source system
	imported_at TIMESTAMP NOT NULL DEFAULT now(),
	CONSTRAINT raw_orders_source_extid UNIQUE (source, external_id)
);