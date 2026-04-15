-- V67: Add PSMPO (combined PSM + PSPO) — private-only 3-day course.
INSERT INTO bagile.course_definitions (code, name, description, duration_days, active)
VALUES (
  'PSMPO',
  'Professional Scrum Master & Product Owner',
  'Combined PSM and PSPO course — 3-day private delivery covering both roles.',
  3,
  TRUE
)
ON CONFLICT (code) DO NOTHING;

UPDATE bagile.course_definitions
SET badge_url = '/badges/PSMPO-course.png'
WHERE code = 'PSMPO' AND badge_url IS NULL;
