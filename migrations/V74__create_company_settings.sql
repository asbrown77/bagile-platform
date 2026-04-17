CREATE TABLE bagile.company_settings (
    id          UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id   VARCHAR(50)  NOT NULL,
    key         VARCHAR(100) NOT NULL,
    value_enc   TEXT         NOT NULL,
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT now(),
    UNIQUE (tenant_id, key)
);

CREATE INDEX idx_company_settings_lookup ON bagile.company_settings (tenant_id);
