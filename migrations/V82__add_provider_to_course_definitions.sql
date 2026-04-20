-- Add provider column to course_definitions.
-- Values: 'scrumorg' | 'icagile' | 'bagile' | NULL (ecommerce-only / unknown)
-- This replaces hardcoded gateway sets scattered across backend and frontend.

ALTER TABLE bagile.course_definitions ADD COLUMN IF NOT EXISTS provider TEXT;

UPDATE bagile.course_definitions
SET provider = 'scrumorg'
WHERE code IN ('APS','PSM','PSPO','APS-SD','PSMA','PAL-E','PSK','PSPOA','PSU','SPS','PAL-EBM','PSFS','PSPBM');

UPDATE bagile.course_definitions
SET provider = 'icagile'
WHERE code IN ('ICP','ICP-ATF','ICP-ACC');

UPDATE bagile.course_definitions
SET provider = 'bagile'
WHERE code IN ('PSM-AI','PSPO-AI','PSM-PO');
