ALTER TABLE bagile.course_definitions
  ADD COLUMN IF NOT EXISTS badge_url TEXT;

-- Seed known local badge paths (portal serves these from /badges/)
UPDATE bagile.course_definitions SET badge_url = '/badges/PSM-I.png'    WHERE code = 'PSM';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSM-II.png'   WHERE code = 'PSMA';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSPO-I.png'   WHERE code = 'PSPO';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSPO-II.png'  WHERE code = 'PSPOA';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSM-AI.png'   WHERE code IN ('PSMAI', 'PSM-AI');
UPDATE bagile.course_definitions SET badge_url = '/badges/PSPO-AI.png'  WHERE code IN ('PSPOAI', 'PSPO-AI');
UPDATE bagile.course_definitions SET badge_url = '/badges/PSK-I.png'    WHERE code IN ('PSK', 'PSK-I');
UPDATE bagile.course_definitions SET badge_url = '/badges/PAL-I.png'    WHERE code IN ('PALE', 'PAL-E');
UPDATE bagile.course_definitions SET badge_url = '/badges/PAL-EBM.png'  WHERE code IN ('PAL-EBM', 'EBM');
UPDATE bagile.course_definitions SET badge_url = '/badges/PSU.png'      WHERE code = 'PSU';
UPDATE bagile.course_definitions SET badge_url = '/badges/PSFS.png'     WHERE code = 'PSFS';
UPDATE bagile.course_definitions SET badge_url = '/badges/APS-SD.png'   WHERE code IN ('APS-SD', 'APSSD');
