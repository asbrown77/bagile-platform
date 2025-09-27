CREATE TABLE IF NOT EXISTS bagile.orders (
  id BIGSERIAL PRIMARY KEY,
  raw_order_id BIGINT REFERENCES bagile.raw_orders(id),
  customer_id BIGINT REFERENCES bagile.students(id),
  total_amount NUMERIC(12,2),
  created_at TIMESTAMPTZ DEFAULT NOW()
);
