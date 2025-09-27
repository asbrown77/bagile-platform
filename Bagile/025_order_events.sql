CREATE TABLE IF NOT EXISTS bagile.order_events (
  id BIGSERIAL PRIMARY KEY,
  order_id BIGINT REFERENCES bagile.orders(id),
  event_type TEXT NOT NULL,  -- transfer, cancellation, refund
  event_date TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  details JSONB
);
