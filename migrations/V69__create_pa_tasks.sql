CREATE TABLE bagile.pa_tasks (
    id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id    VARCHAR(50) NOT NULL DEFAULT 'bagile',
    user_id      VARCHAR(50) NOT NULL,
    type         VARCHAR(100) NOT NULL,
    title        TEXT        NOT NULL,
    payload      JSONB       NOT NULL DEFAULT '{}',
    status       VARCHAR(20) NOT NULL DEFAULT 'open',
    created_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    completed_at TIMESTAMPTZ
);

CREATE INDEX idx_pa_tasks_status ON bagile.pa_tasks (tenant_id, user_id, status);
