CREATE TABLE bagile.pa_user_credentials (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id     VARCHAR(100) NOT NULL,
    tenant_id   VARCHAR(50)  NOT NULL,
    key         VARCHAR(100) NOT NULL,
    value_enc   TEXT         NOT NULL,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    UNIQUE (user_id, tenant_id, key)
);

CREATE INDEX idx_pa_user_credentials_lookup ON bagile.pa_user_credentials (user_id, tenant_id);
