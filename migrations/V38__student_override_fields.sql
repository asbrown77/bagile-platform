-- V38: Student override flag — prevents ETL from overwriting manually corrected data
-- Sprint 16: P1 attendee override feature
--
-- Design: a single JSONB column tracks which fields are overridden.
-- ETL checks this before updating each field. Portal sets/clears flags.
-- Example: {"email": true, "first_name": true}

ALTER TABLE bagile.students
    ADD COLUMN IF NOT EXISTS overridden_fields JSONB NOT NULL DEFAULT '{}',
    ADD COLUMN IF NOT EXISTS updated_by        VARCHAR(100),
    ADD COLUMN IF NOT EXISTS override_note     TEXT;

COMMENT ON COLUMN bagile.students.overridden_fields IS
    'JSONB map of field names that have been manually overridden. ETL skips overridden fields. '
    'Example: {"email": true, "first_name": true}';

COMMENT ON COLUMN bagile.students.updated_by IS
    'Email or identifier of person who last manually updated this record';

COMMENT ON COLUMN bagile.students.override_note IS
    'Optional note explaining why the record was manually overridden (e.g. "PTN partner email corrected to actual attendee")';
