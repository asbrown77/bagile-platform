-- V70: Remove phantom planned course id=1
-- This record has an empty course_type and a default/zero start_date (0001-01-01),
-- indicating it was created with uninitialized data. It shows in the portal as
-- "Mon 1 Jan" with a "?" course type — clearly invalid.

DELETE FROM bagile.course_publications WHERE planned_course_id = 1;
DELETE FROM bagile.planned_courses WHERE id = 1 AND course_type = '' AND start_date = '0001-01-01';
