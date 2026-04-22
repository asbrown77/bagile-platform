-- V83: Backfill course_publications.course_schedule_id for rows linked via planned_course
--
-- Problem: the planned-courses publish flow stores course_publications rows against
-- planned_course_id only. When the ETL later ingests the matching WooCommerce product
-- and creates a course_schedules row, nothing backfills course_schedule_id. As a result,
-- CalendarQueries.GetLiveCoursesAsync could not find scrum.org URLs by course_schedule_id.
--
-- The original V57 CHECK constraint forbade both fields being set at once. Relaxing it
-- to "at least one set" lets a single publication row reflect both its origin (planned)
-- and its current materialisation (schedule). This matches reality and lets us simplify
-- downstream queries.
--
-- After this migration, CalendarQueries still has a UNION ALL safety net to handle any
-- race window between ETL sync and a subsequent publish (e.g. scrum.org published after
-- the schedule exists).

-- 1. Relax the mutual-exclusion CHECK constraint
ALTER TABLE bagile.course_publications DROP CONSTRAINT IF EXISTS chk_one_course_ref;
ALTER TABLE bagile.course_publications ADD CONSTRAINT chk_one_course_ref
    CHECK (planned_course_id IS NOT NULL OR course_schedule_id IS NOT NULL);

-- 2. Backfill course_schedule_id on planned-course publications where a matching
--    course_schedule exists (via shared WooCommerce product ID on the ecommerce row).
--    Skip rows where setting course_schedule_id would violate uq_schedule_gateway.
UPDATE bagile.course_publications cp
SET course_schedule_id = mapping.course_schedule_id
FROM (
    SELECT DISTINCT cp_ecom.planned_course_id, cs.id AS course_schedule_id
    FROM bagile.course_publications cp_ecom
    JOIN bagile.course_schedules cs
        ON cs.source_product_id = cp_ecom.woocommerce_product_id
    WHERE cp_ecom.gateway = 'ecommerce'
      AND cp_ecom.planned_course_id IS NOT NULL
      AND cp_ecom.woocommerce_product_id IS NOT NULL
      AND cs.source_product_id IS NOT NULL
) AS mapping
WHERE cp.planned_course_id = mapping.planned_course_id
  AND cp.course_schedule_id IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM bagile.course_publications existing
      WHERE existing.course_schedule_id = mapping.course_schedule_id
        AND existing.gateway = cp.gateway
        AND existing.id <> cp.id
  );
