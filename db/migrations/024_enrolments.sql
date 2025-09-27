CREATE TABLE IF NOT EXISTS bagile.enrolments (
  id BIGSERIAL PRIMARY KEY,
  student_id BIGINT REFERENCES bagile.students(id),
  course_id BIGINT REFERENCES bagile.courses(id),
  order_id BIGINT REFERENCES bagile.orders(id),
  enrolled_at TIMESTAMPTZ DEFAULT NOW()
);
