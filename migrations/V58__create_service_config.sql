CREATE TABLE bagile.service_config (
    key         VARCHAR(100) PRIMARY KEY,
    value       TEXT         NOT NULL DEFAULT '',
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT now()
);

INSERT INTO bagile.service_config (key, value) VALUES
    ('woocommerce.consumer_key',    ''),
    ('woocommerce.consumer_secret', ''),
    ('scrumorg.username',           ''),
    ('scrumorg.password',           '')
ON CONFLICT (key) DO NOTHING;
