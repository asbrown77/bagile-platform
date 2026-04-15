-- V63: Add missing AI-variant and EBM course definitions
-- PSMAI and PSPOAI were in the portal COURSE_CODES list but never in course_definitions.
-- EBM is distinct from PAL-EBM — it is a standalone Evidence-Based Management workshop.
-- Also seeds badge URLs for APS (missing from V60) and the new rows.

INSERT INTO bagile.course_definitions (code, name, description, duration_days, active)
VALUES
  ('PSMAI',  'Professional Scrum Master with AI Essentials',           'PSM combined with AI tools and practices for Scrum teams.', 2, TRUE),
  ('PSPOAI', 'Professional Scrum Product Owner with AI Essentials',    'PSPO combined with AI tools for product discovery and delivery.', 2, TRUE),
  ('EBM',    'Evidence-Based Management',                              'Using EBM metrics and goals to improve value delivery.', 1, TRUE)
ON CONFLICT (code) DO NOTHING;

-- Seed badge URLs for the new rows
UPDATE bagile.course_definitions SET badge_url = '/badges/PSM-AI.png'  WHERE code = 'PSMAI'  AND badge_url IS NULL;
UPDATE bagile.course_definitions SET badge_url = '/badges/PSPO-AI.png' WHERE code = 'PSPOAI' AND badge_url IS NULL;
UPDATE bagile.course_definitions SET badge_url = '/badges/PAL-EBM.png' WHERE code = 'EBM'    AND badge_url IS NULL;

-- Fill missing APS badge URL (V60 omitted it)
UPDATE bagile.course_definitions SET badge_url = '/badges/APS.png'     WHERE code = 'APS'    AND badge_url IS NULL;
