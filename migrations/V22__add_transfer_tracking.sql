-- Add status and transfer tracking columns
ALTER TABLE bagile.enrolments
    ADD COLUMN IF NOT EXISTS status VARCHAR(20) DEFAULT 'active',
    ADD COLUMN IF NOT EXISTS transferred_to_enrolment_id BIGINT NULL,
    ADD COLUMN IF NOT EXISTS transferred_from_enrolment_id BIGINT NULL,
    ADD COLUMN IF NOT EXISTS original_sku TEXT NULL,
    ADD COLUMN IF NOT EXISTS transfer_reason VARCHAR(30) NULL,
    ADD COLUMN IF NOT EXISTS transfer_notes TEXT NULL,
    ADD COLUMN IF NOT EXISTS refund_eligible BOOLEAN NULL;

-- Add constraints
ALTER TABLE bagile.enrolments
    ADD CONSTRAINT enrolments_status_check
    CHECK (status IN ('active', 'transferred', 'cancelled'));

ALTER TABLE bagile.enrolments
    ADD CONSTRAINT enrolments_transfer_reason_check
    CHECK (transfer_reason IN ('course_cancelled', 'attendee_requested', NULL));

-- Foreign keys for transfer chain
ALTER TABLE bagile.enrolments
    ADD CONSTRAINT enrolments_transferred_to_fk
    FOREIGN KEY (transferred_to_enrolment_id)
    REFERENCES bagile.enrolments(id)
    ON DELETE SET NULL;

ALTER TABLE bagile.enrolments
    ADD CONSTRAINT enrolments_transferred_from_fk
    FOREIGN KEY (transferred_from_enrolment_id)
    REFERENCES bagile.enrolments(id)
    ON DELETE SET NULL;

-- Update unique constraint (allow multiple courses per student per order)
ALTER TABLE bagile.enrolments
    DROP CONSTRAINT IF EXISTS enrolments_unique_student_order_course;

ALTER TABLE bagile.enrolments
    ADD CONSTRAINT enrolments_unique_student_order_course
    UNIQUE (student_id, order_id, course_schedule_id);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_enrolments_status
    ON bagile.enrolments(status);

CREATE INDEX IF NOT EXISTS idx_enrolments_transferred_to
    ON bagile.enrolments(transferred_to_enrolment_id)
    WHERE transferred_to_enrolment_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_enrolments_original_sku
    ON bagile.enrolments(student_id, original_sku)
    WHERE original_sku IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_enrolments_refund_eligible
    ON bagile.enrolments(refund_eligible)
    WHERE refund_eligible IS NOT NULL;

-- View for active enrolments only
CREATE OR REPLACE VIEW bagile.active_enrolments AS
SELECT
    e.id,
    e.student_id,
    e.order_id,
    e.course_schedule_id,
    s.email AS student_email,
    s.first_name AS student_first_name,
    s.last_name AS student_last_name,
    cs.name AS course_name,
    cs.sku AS course_sku,
    cs.start_date AS course_start_date,
    e.original_sku,
    e.transfer_reason,
    e.refund_eligible
FROM bagile.enrolments e
JOIN bagile.students s ON e.student_id = s.id
LEFT JOIN bagile.course_schedules cs ON e.course_schedule_id = cs.id
WHERE e.status = 'active';

COMMENT ON VIEW bagile.active_enrolments IS
    'Shows only active enrolments (excludes transferred and cancelled)';