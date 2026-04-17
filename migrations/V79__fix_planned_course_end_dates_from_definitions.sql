-- V79: Fix planned courses where end_date = start_date but course_definitions
-- says the course runs longer. Catches any courses created after V68 that
-- still have end_date wrongly set to start_date.
--
-- Joins to course_definitions by normalised code (strip hyphens, uppercase)
-- so e.g. "PSM-AI" matches code "PSM-AI" or "PSMAI" in definitions.

UPDATE bagile.planned_courses pc
SET end_date = pc.start_date + (cd.duration_days - 1) * INTERVAL '1 day'
FROM bagile.course_definitions cd
WHERE pc.end_date = pc.start_date
  AND cd.duration_days > 1
  AND cd.active = true
  AND UPPER(REPLACE(REPLACE(pc.course_type, '-', ''), '_', ''))
    = UPPER(REPLACE(REPLACE(cd.code, '-', ''), '_', ''));
