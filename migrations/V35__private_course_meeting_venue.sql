-- V35: Add meeting/venue details and invoice reference for private courses

ALTER TABLE bagile.course_schedules
    ADD COLUMN IF NOT EXISTS invoice_reference TEXT,
    ADD COLUMN IF NOT EXISTS meeting_url TEXT,
    ADD COLUMN IF NOT EXISTS meeting_id TEXT,
    ADD COLUMN IF NOT EXISTS meeting_passcode TEXT,
    ADD COLUMN IF NOT EXISTS venue_address TEXT;
