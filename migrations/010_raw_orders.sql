CREATE TABLE bagile.raw_orders (
    id SERIAL PRIMARY KEY,
    source TEXT NOT NULL,
    external_id TEXT NOT NULL,
    payload JSONB NOT NULL,
    imported_at TIMESTAMP DEFAULT now(),
    UNIQUE (source, external_id, payload)  
);
