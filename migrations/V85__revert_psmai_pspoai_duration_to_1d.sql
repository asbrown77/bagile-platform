-- V85: Revert V84's incorrect change — PSMAI and PSPOAI are 1-day courses.
-- V84 wrongly set duration_days = 2 and set end_date = start_date + 1 day.

-- 1. Restore duration_days = 1
UPDATE bagile.course_definitions
SET duration_days = 1
WHERE code IN ('PSMAI', 'PSPOAI');

-- 2. Revert planned_courses end_date back to start_date
UPDATE bagile.planned_courses
SET end_date = start_date
WHERE end_date = start_date + INTERVAL '1 day'
  AND UPPER(REPLACE(course_type, '-', '')) IN ('PSMAI', 'PSPOAI');

-- 3. Revert course_schedules end_date back to start_date
UPDATE bagile.course_schedules
SET end_date = start_date
WHERE end_date = start_date + INTERVAL '1 day'
  AND start_date IS NOT NULL
  AND (
    sku ILIKE 'PSMAI-%'
    OR sku ILIKE '%-PSMAI-%'
    OR sku ILIKE 'PSPOAI-%'
    OR sku ILIKE '%-PSPOAI-%'
  );
