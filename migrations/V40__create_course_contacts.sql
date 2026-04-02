-- V40: Course contacts for private courses
-- Stores logistics/admin contacts separately from attendees.

CREATE TABLE IF NOT EXISTS bagile.course_contacts (
    id          BIGSERIAL PRIMARY KEY,
    course_schedule_id BIGINT NOT NULL REFERENCES bagile.course_schedules(id) ON DELETE CASCADE,
    role        TEXT NOT NULL CHECK (role IN ('admin', 'organiser', 'other')),
    name        TEXT NOT NULL,
    email       TEXT NOT NULL,
    phone       TEXT,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_course_contacts_schedule
    ON bagile.course_contacts (course_schedule_id);
