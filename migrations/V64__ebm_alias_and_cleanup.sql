-- V64: EBM is an alias for PAL-EBM, not a separate course definition.
-- Remove the standalone EBM row added in V63 and register EBM as a canonical alias.
-- Also clears the APS badge_url (no badge image file exists for standalone APS).

-- 1. Register EBM as an alias for PAL-EBM
INSERT INTO bagile.course_code_aliases (alias, canonical_code)
VALUES ('EBM', 'PAL-EBM')
ON CONFLICT (alias) DO UPDATE SET canonical_code = 'PAL-EBM';

-- 2. Delete the standalone EBM course definition added in V63
DELETE FROM bagile.course_definitions WHERE code = 'EBM';

-- 3. Clear the APS badge URL — no badge image file exists for standalone APS
UPDATE bagile.course_definitions SET badge_url = NULL WHERE code = 'APS' AND badge_url = '/badges/APS.png';
