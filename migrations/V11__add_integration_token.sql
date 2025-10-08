DROP TABLE IF EXISTS bagile.integration_tokens;

CREATE TABLE bagile.integration_tokens (
    source TEXT PRIMARY KEY,
    refresh_token TEXT NOT NULL,
    access_token TEXT,
    external_account_id TEXT,
    expires_at TIMESTAMPTZ
);
