-- V34: Support private course data entry
-- Private courses are created via the portal (not WooCommerce).
-- Attendees are registered directly — no WooCommerce order exists.

-- 1. Allow enrolments without an order (private course attendees)
ALTER TABLE bagile.enrolments ALTER COLUMN order_id DROP NOT NULL;

-- 2. Add source tracking to enrolments
ALTER TABLE bagile.enrolments ADD COLUMN IF NOT EXISTS source TEXT DEFAULT 'woo';

-- 3. Replace single unique constraint with partial indexes
--    (PostgreSQL treats NULLs as distinct in unique indexes)
ALTER TABLE bagile.enrolments DROP CONSTRAINT IF EXISTS enrolments_unique_student_order_course;

CREATE UNIQUE INDEX IF NOT EXISTS idx_enrolments_unique_with_order
    ON bagile.enrolments (student_id, order_id, course_schedule_id)
    WHERE order_id IS NOT NULL;

CREATE UNIQUE INDEX IF NOT EXISTS idx_enrolments_unique_without_order
    ON bagile.enrolments (student_id, course_schedule_id)
    WHERE order_id IS NULL;

-- 4. Private course metadata on course_schedules
ALTER TABLE bagile.course_schedules
    ADD COLUMN IF NOT EXISTS client_organisation_id BIGINT
        REFERENCES bagile.organisations(id),
    ADD COLUMN IF NOT EXISTS notes TEXT,
    ADD COLUMN IF NOT EXISTS created_by TEXT;
