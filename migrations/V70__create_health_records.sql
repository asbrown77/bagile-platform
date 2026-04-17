CREATE TABLE bagile.health_records (
    id             UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    automation_name VARCHAR(100) NOT NULL,
    tenant_id      VARCHAR(50)  NOT NULL,
    run_at         TIMESTAMPTZ  NOT NULL DEFAULT now(),
    status         VARCHAR(20)  NOT NULL,
    duration_ms    INTEGER      NOT NULL,
    error_message  TEXT,
    triggered_by   VARCHAR(20)  NOT NULL DEFAULT 'manual'
);

CREATE INDEX idx_health_records_lookup ON bagile.health_records (automation_name, tenant_id, run_at DESC);
CREATE INDEX idx_health_records_tenant ON bagile.health_records (tenant_id, run_at DESC);
