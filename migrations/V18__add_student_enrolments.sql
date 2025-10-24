
ALTER TABLE bagile.raw_orders ADD COLUMN IF NOT EXISTS error_message TEXT;

-- =======================================================
-- 1. STUDENTS
-- =======================================================
DROP TABLE IF EXISTS bagile.students CASCADE;

CREATE TABLE bagile.students (
    id BIGSERIAL PRIMARY KEY,
    email TEXT NOT NULL,
    first_name TEXT,
    last_name TEXT,
    company TEXT,
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT uq_students_email UNIQUE (email)
);

-- =======================================================
-- 2. ENROLMENTS
-- =======================================================
DROP TABLE IF EXISTS bagile.enrolments CASCADE;

CREATE TABLE bagile.enrolments (
    id BIGSERIAL PRIMARY KEY,
    student_id BIGINT NOT NULL REFERENCES bagile.students(id) ON DELETE CASCADE,
    order_id BIGINT NOT NULL REFERENCES bagile.orders(id) ON DELETE CASCADE,
    course_schedule_id BIGINT REFERENCES bagile.course_schedules(id),
    source TEXT NOT NULL DEFAULT 'woo',
    created_at TIMESTAMP DEFAULT NOW(),
    updated_at TIMESTAMP DEFAULT NOW(),
    CONSTRAINT uq_enrolment UNIQUE (student_id, order_id, course_schedule_id)
);
