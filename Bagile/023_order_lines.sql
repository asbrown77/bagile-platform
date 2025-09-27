CREATE TABLE IF NOT EXISTS bagile.order_lines (
  id BIGSERIAL PRIMARY KEY,
  order_id BIGINT REFERENCES bagile.orders(id),
  product_code TEXT,
  quantity INT NOT NULL DEFAULT 1,
  price NUMERIC(12,2)
);
