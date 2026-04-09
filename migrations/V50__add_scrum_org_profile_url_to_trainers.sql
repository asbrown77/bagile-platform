-- V50: Add scrum_org_profile_url to trainers table
-- Used in follow-up emails so the feedback link points to the correct trainer profile.

ALTER TABLE bagile.trainers
    ADD COLUMN IF NOT EXISTS scrum_org_profile_url VARCHAR(500);

UPDATE bagile.trainers SET scrum_org_profile_url = 'https://www.scrum.org/find-trainers/alex-brown'
WHERE email = 'alexbrown@bagile.co.uk';

UPDATE bagile.trainers SET scrum_org_profile_url = 'https://www.scrum.org/find-trainers/chris-bexon'
WHERE email = 'chrisbexon@bagile.co.uk';
