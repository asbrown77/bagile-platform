-- V62: Remove phantom private course record (id=735, title "course 735")
-- This was a test/phantom record with no meaningful data.

-- Remove any enrolments first (referential integrity)
DELETE FROM bagile.enrolments WHERE course_schedule_id = 735;

-- Delete the phantom course schedule
DELETE FROM bagile.course_schedules WHERE id = 735 AND is_public = false;
