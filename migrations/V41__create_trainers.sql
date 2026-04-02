-- V41: Trainers table
-- Replaces free-text trainer names on courses with a managed list.
-- Note: course_schedules.trainer_name remains VARCHAR (no FK) for backward
-- compatibility with ETL-synced public courses and existing data.

CREATE TABLE IF NOT EXISTS bagile.trainers (
    id         SERIAL PRIMARY KEY,
    name       VARCHAR(200) NOT NULL,
    email      VARCHAR(200) NOT NULL UNIQUE,
    phone      VARCHAR(50),
    is_active  BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

INSERT INTO bagile.trainers (name, email) VALUES
    ('Alex Brown',   'alexbrown@bagile.co.uk'),
    ('Chris Bexon',  'chrisbexon@bagile.co.uk')
ON CONFLICT (email) DO NOTHING;
