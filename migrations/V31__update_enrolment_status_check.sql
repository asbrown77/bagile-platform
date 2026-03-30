-- V31: Expand enrolment status to support refund and pending transfer workflows

ALTER TABLE bagile.enrolments DROP CONSTRAINT IF EXISTS enrolments_status_check;
ALTER TABLE bagile.enrolments ADD CONSTRAINT enrolments_status_check
    CHECK (status IN ('active', 'transferred', 'cancelled', 'pending_transfer', 'refunded'));

ALTER TABLE bagile.enrolments DROP CONSTRAINT IF EXISTS enrolments_transfer_reason_check;
ALTER TABLE bagile.enrolments ADD CONSTRAINT enrolments_transfer_reason_check
    CHECK (transfer_reason IN ('course_cancelled', 'attendee_requested', 'CourseTransfer', 'provider_cancelled') OR transfer_reason IS NULL);
