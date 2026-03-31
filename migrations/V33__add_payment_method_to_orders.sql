-- V33: Add payment method tracking to orders
-- WooCommerce provides payment_method (e.g. 'stripe', 'cod') and payment_method_title (e.g. 'Credit Card', 'Invoice payment')

ALTER TABLE bagile.orders
    ADD COLUMN IF NOT EXISTS payment_method TEXT,
    ADD COLUMN IF NOT EXISTS payment_method_title TEXT;
