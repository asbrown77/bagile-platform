-- Record the scrum.org publication for APSSD-250526-AB (schedule 44).
-- This course was created manually on scrum.org (node 106837) before the
-- automated publish flow was available for APSSD.

INSERT INTO bagile.course_publications (course_schedule_id, gateway, published_at, external_url)
VALUES (44, 'scrumorg', NOW(), 'https://www.scrum.org/courses/applying-professional-scrum-software-development-2026-05-25-106837')
ON CONFLICT (course_schedule_id, gateway)
DO UPDATE SET
    external_url = EXCLUDED.external_url,
    published_at = EXCLUDED.published_at;
