-- V28: API key management — supports multiple named keys per user
-- Keys are stored as SHA-256 hashes; raw key shown only at creation time.

CREATE TABLE IF NOT EXISTS bagile.api_keys (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    key_hash        TEXT NOT NULL UNIQUE,
    key_prefix      TEXT NOT NULL,              -- first 12 chars for display (e.g. "bgl_a1b2c3d4")
    owner_email     TEXT NOT NULL,
    owner_name      TEXT NOT NULL,
    label           TEXT,                       -- user-chosen name ("MCP server", "testing")
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_used_at    TIMESTAMPTZ,
    revoked_at      TIMESTAMPTZ
);

CREATE INDEX idx_api_keys_hash_active ON bagile.api_keys (key_hash) WHERE is_active = TRUE;
CREATE INDEX idx_api_keys_owner ON bagile.api_keys (owner_email);

-- Note: The existing API key continues to work via the legacy config fallback
-- in ApiKeyAuthenticationMiddleware. New keys are created through the portal.
