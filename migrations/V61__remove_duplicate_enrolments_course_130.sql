-- V61: Cancel orphaned duplicate enrolments on course_schedule_id 130
--
-- Background: A WooCommerce attendee email correction caused the ETL to
-- create duplicate enrolment rows for course_schedule_id 130 because the
-- old UpsertAsync matched on student_id+order_id+course_schedule_id. When
-- the email changed, a new student row was created and the upsert inserted
-- a second enrolment instead of updating the existing one.
-- Enrolment IDs 1796, 15, and 19 are the orphaned duplicates (the valid
-- enrolments for those seats already exist under the updated student_ids).
-- We cancel rather than hard-delete to preserve the audit trail.

UPDATE bagile.enrolments
SET status       = 'cancelled',
    is_cancelled = TRUE,
    cancelled_at = NOW(),
    updated_at   = NOW()
WHERE id IN (1796, 15, 19)
  AND course_schedule_id = 130;
