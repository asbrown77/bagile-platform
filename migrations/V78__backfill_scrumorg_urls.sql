-- Backfill scrum.org course listing URLs into course_publications for all
-- active upcoming course_schedules. These courses pre-date the publication
-- tracking system; the scrumorg gateway already shows Published=true.
-- Adding external_url makes the portal gateway checklist link clickable.
--
-- Scraped from https://www.scrum.org/admin/courses/manage on 2026-04-17.
-- Cancelled courses (e.g. PSPO-270426-AB id=22) are excluded.

INSERT INTO bagile.course_publications (course_schedule_id, gateway, published_at, external_url)
VALUES
    (16,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-agile-leadership-essentials-2026-04-20-101287'),
    (17,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-master-2026-04-20-101271'),
    (19,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-master-advanced-2026-04-23-101304'),
    (25,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-agile-leadership-evidence-based-management-2026-05-01-101512'),
    (26,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-facilitation-skills-2026-05-04-101527'),
    (27,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-master-2026-05-04-101260'),
    (30,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-kanban-2026-05-07-101302'),
    (32,  'scrumorg', NOW(), 'https://www.scrum.org/courses/applying-professional-scrum-2026-05-11-101522'),
    (33,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-user-experience-2026-05-11-101298'),
    (35,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-agile-leadership-essentials-2026-05-14-101516'),
    (36,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-product-owner-2026-05-14-101256'),
    (37,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-agile-leadership-evidence-based-management-2026-05-18-101509'),
    (41,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-product-owner-advanced-2026-05-20-101296'),
    (42,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-master-2026-05-21-101272'),
    (43,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-kanban-2026-05-25-101524'),
    (858, 'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-master-advanced-2026-05-28-106768'),
    (47,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-scrum-facilitation-skills-2026-06-01-101528'),
    (49,  'scrumorg', NOW(), 'https://www.scrum.org/courses/professional-agile-leadership-essentials-2026-06-04-101288')
ON CONFLICT (course_schedule_id, gateway)
DO UPDATE SET
    external_url = EXCLUDED.external_url,
    published_at = EXCLUDED.published_at;
