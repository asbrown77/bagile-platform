-- V83: Re-apply end_date fix for any planned_courses created after V79 ran
-- where end_date = start_date but course_definitions says the course runs longer.
-- Covers the same logic as V68 and V79 but catches records created after those
-- migrations were applied (e.g. AI courses bulk-created via CSV or early portal).

UPDATE bagile.planned_courses pc
SET end_date = pc.start_date + (cd.duration_days - 1) * INTERVAL '1 day'
FROM bagile.course_definitions cd
WHERE pc.end_date = pc.start_date
  AND cd.duration_days > 1
  AND cd.active = true
  AND UPPER(REPLACE(REPLACE(pc.course_type, '-', ''), '_', ''))
    = UPPER(REPLACE(REPLACE(cd.code, '-', ''), '_', ''));
