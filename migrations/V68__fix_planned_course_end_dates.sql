-- V68: Fix planned courses where end_date = start_date but course type runs longer.
-- Courses entered before the portal auto-calculated end dates from duration may have
-- end_date stored as the same day as start_date.
--
-- Duration rules (matching course_definitions.duration_days):
--   3-day courses: APSSD
--   2-day courses: all standard Scrum.org and ICAgile courses (PSM, PSPO, PALE, APS, etc.)
--   1-day courses: left unchanged (end_date = start_date is correct)

-- Fix 3-day courses (end_date should be start_date + 2 days)
UPDATE bagile.planned_courses
SET end_date = start_date + INTERVAL '2 days'
WHERE end_date = start_date
  AND course_type IN ('APSSD', 'APS-SD');

-- Fix 2-day courses (end_date should be start_date + 1 day)
UPDATE bagile.planned_courses
SET end_date = start_date + INTERVAL '1 day'
WHERE end_date = start_date
  AND course_type IN (
    'PSM', 'PSMO', 'PSMA', 'PSMAI',
    'PSPO', 'PSPOA', 'PSPOAI',
    'PSK', 'PALE', 'PAL', 'PALEBM',
    'APS', 'PSU', 'PSFS',
    'ICP', 'ICPATF', 'ICPACC',
    'PSMPO'
  );
