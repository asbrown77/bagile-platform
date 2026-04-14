-- V56: planned_courses table — portal-only scheduling intent
-- Part of Sprint 26: Course Calendar v1

CREATE TABLE bagile.planned_courses (
    id              SERIAL PRIMARY KEY,
    course_type     VARCHAR(20)  NOT NULL,       -- e.g. PSMA, PSPO, PSMAI (no hyphens)
    trainer_id      INT          NOT NULL REFERENCES bagile.trainers(id),
    start_date      DATE         NOT NULL,
    end_date        DATE         NOT NULL,
    is_virtual      BOOLEAN      NOT NULL DEFAULT TRUE,
    venue           VARCHAR(255),
    notes           TEXT,
    decision_deadline DATE,                      -- NULL => app defaults to start_date - 10 days
    is_private      BOOLEAN      NOT NULL DEFAULT FALSE,
    status          VARCHAR(20)  NOT NULL DEFAULT 'planned'
                    CHECK (status IN ('planned', 'cancelled')),
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_planned_courses_start_date ON bagile.planned_courses(start_date);
CREATE INDEX idx_planned_courses_status     ON bagile.planned_courses(status);
CREATE INDEX idx_planned_courses_trainer    ON bagile.planned_courses(trainer_id);

-- Trigger to auto-update updated_at on row change
CREATE OR REPLACE FUNCTION bagile.set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_planned_courses_updated_at
    BEFORE UPDATE ON bagile.planned_courses
    FOR EACH ROW EXECUTE FUNCTION bagile.set_updated_at();
