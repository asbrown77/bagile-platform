CREATE TABLE IF NOT EXISTS bagile.courses (
  id BIGSERIAL PRIMARY KEY,
  code TEXT NOT NULL,
  name TEXT NOT NULL,
  start_date DATE,
  trainer TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_courses_code 
  ON bagile.courses(code);
