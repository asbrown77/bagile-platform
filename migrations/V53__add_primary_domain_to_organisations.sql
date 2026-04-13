-- V46: Add primary_domain to organisations table
-- Sprint 25: Organisation Data Quality — allows known orgs to override
-- auto-detected domain (which derives from student emails, not the org itself).
-- Also expands aliases for key partners to consolidate duplicate portal entries.

ALTER TABLE bagile.organisations ADD COLUMN IF NOT EXISTS primary_domain TEXT;

-- Seed primary domains for known organisations
UPDATE bagile.organisations SET primary_domain = 'qa.com'                WHERE name = 'QA Ltd';
UPDATE bagile.organisations SET primary_domain = 'nobleprog.com'         WHERE name = 'NobleProg';
UPDATE bagile.organisations SET primary_domain = 'knowledgetrain.co.uk'  WHERE name = 'Knowledge Train';
UPDATE bagile.organisations SET primary_domain = 'learnquest.com'        WHERE name = 'LearnQuest';
UPDATE bagile.organisations SET primary_domain = 'indiciatraining.com'   WHERE name = 'Indicia Training';
UPDATE bagile.organisations SET primary_domain = 'k21academy.com'        WHERE name = 'K21';
UPDATE bagile.organisations SET primary_domain = 'invensislearning.com'  WHERE name = 'Invensis';
UPDATE bagile.organisations SET primary_domain = 'theknowledgeacademy.com' WHERE name = 'The Knowledge Academy';
UPDATE bagile.organisations SET primary_domain = 'scrumtrainer.co.uk'    WHERE name = 'Scrum Trainer';
UPDATE bagile.organisations SET primary_domain = 'optilearn.co.uk'       WHERE name = 'Optilearn';
UPDATE bagile.organisations SET primary_domain = 'nellcote.co.uk'        WHERE name = 'Nellcote';
UPDATE bagile.organisations SET primary_domain = 'elitetraining.co.uk'   WHERE name = 'Elite Training';
UPDATE bagile.organisations SET primary_domain = 'agilityarabia.com'     WHERE name = 'Agility Arabia';
UPDATE bagile.organisations SET primary_domain = 'scopphu.com'           WHERE name = 'Scopphu';
UPDATE bagile.organisations SET primary_domain = 'bhf.org.uk'            WHERE name = 'BHF';
UPDATE bagile.organisations SET primary_domain = 'jisc.ac.uk'            WHERE name = 'JISC';
UPDATE bagile.organisations SET primary_domain = 'bagile.co.uk'          WHERE name = 'BAgile';

-- Expand NobleProg aliases to consolidate all billing_company variations
UPDATE bagile.organisations
SET aliases = '{"NobleProg","NOBLEPROG (UK) LTD","NobleProg (UK) Ltd","nobleprog.co.uk","nobleprog.com"}'
WHERE name = 'NobleProg';

-- Expand QA Ltd aliases to consolidate all billing_company variations
UPDATE bagile.organisations
SET aliases = '{"QA Ltd","QA Limited","QA LTD","qa.com"}'
WHERE name = 'QA Ltd';

-- Expand BAgile aliases
UPDATE bagile.organisations
SET aliases = '{"BAgile","BAgile Limited","b-agile","Bagile Limited","BAgile Ltd","bagile.co.uk","b-agile ltd"}'
WHERE name = 'BAgile';

-- Expand BHF aliases
UPDATE bagile.organisations
SET aliases = '{"BHF","BHF.org.uk","British Heart Foundation","bhf.org.uk","BHF UK"}'
WHERE name = 'BHF';

-- Expand JISC aliases
UPDATE bagile.organisations
SET aliases = '{"JISC","Jisc","jisc"}'
WHERE name = 'JISC';
