-- V26: Clean up course_schedule data quality issues
-- Sprint 4 (27 Mar 2026) — data integrity

-- 1. Normalize empty-string SKUs to NULL
UPDATE bagile.course_schedules
SET sku = NULL
WHERE sku = '';

-- 2. Fix inconsistent SKU: APSSD should be APS-SD
-- This affects minimum calculation in monitoring endpoint
UPDATE bagile.course_schedules
SET sku = REPLACE(sku, 'APSSD-', 'APS-SD-')
WHERE sku LIKE 'APSSD-%';

-- 3. Add course_type and course_definition_id columns if missing
-- (course_type used for interactive course detection)
ALTER TABLE bagile.course_schedules
ADD COLUMN IF NOT EXISTS course_type TEXT;

-- 4. Report: courses with null start_date (for manual review, not auto-fix)
-- Run this to see them: SELECT id, name, sku, status FROM bagile.course_schedules WHERE start_date IS NULL;
