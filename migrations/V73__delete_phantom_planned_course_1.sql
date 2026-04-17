-- V71 did not remove this record (conditions may not have matched at run time).
-- Delete the phantom planned course id=1 unconditionally.
DELETE FROM bagile.course_publications WHERE planned_course_id = 1;
DELETE FROM bagile.planned_courses WHERE id = 1;
