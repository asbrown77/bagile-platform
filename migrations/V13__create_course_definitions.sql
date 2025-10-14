-- Drop old courses table if it exists
DROP TABLE IF EXISTS courses CASCADE;

-- Create new course_definitions table
CREATE TABLE course_definitions (
    id SERIAL PRIMARY KEY,
    code TEXT UNIQUE NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    duration_days INT DEFAULT 2,
    base_price NUMERIC(10,2),
    active BOOLEAN DEFAULT TRUE
);

-- Add link from course_schedules to course_definitions
ALTER TABLE course_schedules
ADD COLUMN course_definition_id INT REFERENCES course_definitions(id);