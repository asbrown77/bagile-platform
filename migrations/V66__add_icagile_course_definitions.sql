-- V66: Add ICAgile course definitions delivered by BAgile.
-- Durations are approximate — adjust via the settings/courses page once confirmed.
-- ICP = 2 days, ICP-ATF = 2 days, ICP-ACC = 3 days (adjust as needed).

INSERT INTO bagile.course_definitions (code, name, description, duration_days, active)
VALUES
  ('ICP',     'ICAgile Certified Professional',          'Agile foundations and mindset. ICAgile ICP certification.', 2, TRUE),
  ('ICP-ATF', 'ICAgile Agile Team Facilitation',         'Facilitation skills for agile teams. ICAgile ICP-ATF certification.', 2, TRUE),
  ('ICP-ACC', 'ICAgile Agile Coaching Certification',    'Agile coaching competencies. ICAgile ICP-ACC certification.', 3, TRUE)
ON CONFLICT (code) DO NOTHING;
