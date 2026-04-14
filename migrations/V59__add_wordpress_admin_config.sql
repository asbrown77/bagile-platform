-- Add WordPress admin credentials for Playwright-based automations
-- (FooEvents ticket management, future wp-admin operations)
-- woocommerce.consumer_key / consumer_secret = WP Application Password (REST API)
-- woocommerce.admin_username / admin_password = WP admin account (Playwright)
INSERT INTO bagile.service_config (key, value) VALUES
    ('woocommerce.admin_username', ''),
    ('woocommerce.admin_password', '')
ON CONFLICT (key) DO NOTHING;
