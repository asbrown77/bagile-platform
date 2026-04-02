-- V45: Add acronym to organisations table
-- Sprint 23: "Professional Private Courses" — supports auto-generated invoice references
-- e.g. PSM-FNC-270426 requires the org acronym (FNC for Frazer-Nash).

ALTER TABLE bagile.organisations ADD COLUMN IF NOT EXISTS acronym VARCHAR(20);

-- Seed known acronyms for existing organisations
UPDATE bagile.organisations SET acronym = 'QA'   WHERE name ILIKE '%QA Ltd%'      OR name ILIKE '%QA Limited%';
UPDATE bagile.organisations SET acronym = 'NP'   WHERE name ILIKE '%NobleProg%';
UPDATE bagile.organisations SET acronym = 'FNC'  WHERE name ILIKE '%Frazer%Nash%';
UPDATE bagile.organisations SET acronym = 'DVSA' WHERE name ILIKE '%DVSA%'         OR name ILIKE '%Driver%Vehicle%';
UPDATE bagile.organisations SET acronym = 'HMLR' WHERE name ILIKE '%Land Registry%';
UPDATE bagile.organisations SET acronym = 'KTN'  WHERE name ILIKE '%Knowledge Transfer%';
UPDATE bagile.organisations SET acronym = 'BHF'  WHERE name ILIKE '%BHF%'          OR name ILIKE '%British Heart%';
UPDATE bagile.organisations SET acronym = 'JISC' WHERE name = 'JISC';
UPDATE bagile.organisations SET acronym = 'LQ'   WHERE name ILIKE '%LearnQuest%';
UPDATE bagile.organisations SET acronym = 'KT'   WHERE name ILIKE '%Knowledge Train%';
UPDATE bagile.organisations SET acronym = 'TKA'  WHERE name ILIKE '%Knowledge Academy%';
UPDATE bagile.organisations SET acronym = 'INV'  WHERE name ILIKE '%Invensis%';
UPDATE bagile.organisations SET acronym = 'ETC'  WHERE name ILIKE '%Elite Training%';
UPDATE bagile.organisations SET acronym = 'OPL'  WHERE name ILIKE '%Optilearn%';
UPDATE bagile.organisations SET acronym = 'BAGILE' WHERE name = 'BAgile';
