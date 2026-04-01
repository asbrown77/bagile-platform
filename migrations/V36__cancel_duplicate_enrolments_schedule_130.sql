-- V36: Cancel orphaned duplicate enrolments on course schedule 130
--
-- These three enrolments were created when attendee emails were corrected
-- in WooCommerce. The ETL created new student records for the new emails
-- and then inserted new enrolments (old student_id + same order + same
-- course_schedule_id = no match → insert). The original enrolments
-- (with the old student IDs) became orphans.
--
-- The fix in EnrolmentRepository.UpsertAsync (matching by order_id +
-- course_schedule_id instead of student_id) prevents this going forward.
-- This migration cleans up the pre-existing orphans.
--
-- Verified 1 Apr 2026: enrolments 1796, 15, 19 all have is_cancelled = false
-- and are duplicated by newer active enrolments for the same orders.

UPDATE bagile.enrolments
SET    is_cancelled = TRUE,
       status       = 'cancelled',
       cancelled_at = NOW(),
       updated_at   = NOW()
WHERE  id IN (1796, 15, 19)
  AND  course_schedule_id = 130
  AND  is_cancelled IS NOT TRUE;
