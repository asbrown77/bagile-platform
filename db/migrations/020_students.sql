CREATE TABLE IF NOT EXISTS bagile.students (
  id BIGSERIAL PRIMARY KEY,
  name TEXT NOT NULL,
  email TEXT NOT NULL,
  external_id TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_students_email 
  ON bagile.students(email);
