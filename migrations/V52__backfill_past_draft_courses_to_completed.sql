-- V52: Backfill past draft courses to completed
-- WooCommerce auto-archives courses back to 'draft' after their date passes.
-- Any course with status='draft' and an end date in the past should be 'completed'.
UPDATE bagile.course_schedules
SET status = 'completed'
WHERE status = 'draft'
  AND COALESCE(end_date, start_date) < CURRENT_DATE;
