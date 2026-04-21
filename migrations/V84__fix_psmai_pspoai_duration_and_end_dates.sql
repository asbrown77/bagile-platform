-- V84: Fix PSMAI and PSPOAI duration_days (were incorrectly set to 1, should be 2).
-- Then repair end_date = start_date in both planned_courses and course_schedules.

-- 1. Fix course_definitions
UPDATE bagile.course_definitions
SET duration_days = 2
WHERE code IN ('PSMAI', 'PSPOAI')
  AND duration_days = 1;

-- 2. Fix planned_courses (V83 missed these because duration_days was 1 at the time)
UPDATE bagile.planned_courses
SET end_date = start_date + INTERVAL '1 day'
WHERE end_date = start_date
  AND UPPER(REPLACE(course_type, '-', '')) IN ('PSMAI', 'PSPOAI');

-- 3. Fix course_schedules (live WooCommerce courses synced with end_date = start_date)
--    SKUs can be PSMAI-DDMMYY-XX or ORG-PSMAI-DDMMYY or PSMAI-DDMMYYYY etc.
UPDATE bagile.course_schedules
SET end_date = start_date + INTERVAL '1 day'
WHERE start_date IS NOT NULL
  AND end_date = start_date
  AND (
    sku ILIKE 'PSMAI-%'
    OR sku ILIKE '%-PSMAI-%'
    OR sku ILIKE 'PSPOAI-%'
    OR sku ILIKE '%-PSPOAI-%'
  );
