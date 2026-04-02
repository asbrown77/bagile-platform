-- V44: Seed pre-course email templates for remaining 9 course types
-- Sprint 21: "Send Joining Details" — covers PSPO, PSK, APS-SD, PAL-E, PSPO-A, PSM-A, PSFS, PSU, EBM
-- Each course type has both a Virtual (zoom details) and F2F (venue address) variant.
--
-- Structure mirrors V42 (PSM templates). Variables substituted at send time:
--   Runtime: {{course_name}}, {{dates}}, {{times}}, {{trainer_name}}, {{client_name}}
--   F2F:     {{venue_address}}
--   Virtual: {{zoom_url}}, {{zoom_id}}, {{zoom_passcode}}
--   Content: {{self_study}}, {{agenda}}  (built from course type in SendPreCourseEmailCommandHandler)
--
-- ON CONFLICT DO NOTHING: safe to re-run; existing customised templates are preserved.

-- ============================================================
-- PSPO — Professional Scrum Product Owner (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSPO',
    'f2f',
    'Professional Scrum Product Owner Training ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Professional Scrum Product Owner class.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSPO',
    'virtual',
    'Professional Scrum Product Owner Training ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Professional Scrum Product Owner class.</p>

<p>The course runs online via Zoom. Your joining details are below. Please join a few minutes early
so we can start on time. Your trainer will be available after the session each day if you have
questions or want to chat further.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

-- ============================================================
-- PSK — Professional Scrum with Kanban (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSK',
    'f2f',
    'Professional Scrum with Kanban Training ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Professional Scrum with Kanban class.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSK',
    'virtual',
    'Professional Scrum with Kanban Training ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Professional Scrum with Kanban class.</p>

<p>The course runs online via Zoom. Your joining details are below. Please join a few minutes early
so we can start on time. Your trainer will be available after the session each day if you have
questions or want to chat further.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

-- ============================================================
-- APS-SD — Applying Professional Scrum for Software Development (3-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'APS-SD',
    'f2f',
    'Applying Professional Scrum for Software Development ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Applying Professional Scrum for Software Development class.</p>

<p>This is a 3-day hands-on course. Please aim to arrive around 10 minutes before the start on Day 1.
Bring your laptop if you plan to code during exercises. Your trainer will be available after each
session if you have questions.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'APS-SD',
    'virtual',
    'Applying Professional Scrum for Software Development ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Applying Professional Scrum for Software Development class.</p>

<p>This is a 3-day hands-on course running online via Zoom. Your joining details are below.
Please join a few minutes early on Day 1. Bring your laptop if you plan to code during exercises.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

-- ============================================================
-- PAL-E — Professional Agile Leadership Essentials (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PAL-E',
    'f2f',
    'Professional Agile Leadership Essentials ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Professional Agile Leadership Essentials class.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PAL-E',
    'virtual',
    'Professional Agile Leadership Essentials ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Professional Agile Leadership Essentials class.</p>

<p>The course runs online via Zoom. Your joining details are below. Please join a few minutes early
so we can start on time. Your trainer will be available after the session each day if you have
questions or want to chat further.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

-- ============================================================
-- PSPO-A — Professional Scrum Product Owner Advanced (1-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSPO-A',
    'f2f',
    'Professional Scrum Product Owner Advanced ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Date:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Professional Scrum Product Owner Advanced class.</p>

<p>This is an advanced 1-day course. It builds on PSPO foundations — we will be going deep into
product strategy and stakeholder complexity. Please aim to arrive around 10 minutes before the start.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before the Day — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSPO-A',
    'virtual',
    'Professional Scrum Product Owner Advanced ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Date:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Professional Scrum Product Owner Advanced class.</p>

<p>This is an advanced 1-day course running online via Zoom. It builds on PSPO foundations — we
will be going deep into product strategy and stakeholder complexity. Your joining details are below.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before the Day — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

-- ============================================================
-- PSM-A — Professional Scrum Master Advanced (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSM-A',
    'f2f',
    'Professional Scrum Master Advanced ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Professional Scrum Master Advanced class.</p>

<p>This is an advanced course designed for experienced Scrum Masters. We will be working through
complex scenarios and coaching challenges. Please aim to arrive around 10 minutes before the start.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSM-A',
    'virtual',
    'Professional Scrum Master Advanced ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Professional Scrum Master Advanced class.</p>

<p>This is an advanced course for experienced Scrum Masters, running online via Zoom. We will be
working through complex scenarios and coaching challenges. Your joining details are below.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

-- ============================================================
-- PSFS — Professional Scrum Facilitation Skills (1-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSFS',
    'f2f',
    'Professional Scrum Facilitation Skills ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Date:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Professional Scrum Facilitation Skills class.</p>

<p>This is a practical 1-day course with lots of exercises. Please aim to arrive around 10 minutes
before the start so we can get settled and begin on time.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before the Day — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSFS',
    'virtual',
    'Professional Scrum Facilitation Skills ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Date:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Professional Scrum Facilitation Skills class.</p>

<p>This is a practical 1-day course with lots of exercises, running online via Zoom.
Please join a few minutes early so we can start on time. Your joining details are below.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before the Day — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

-- ============================================================
-- PSU — Professional Scrum with User Experience (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSU',
    'f2f',
    'Professional Scrum with User Experience ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Professional Scrum with User Experience class.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSU',
    'virtual',
    'Professional Scrum with User Experience ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Professional Scrum with User Experience class.</p>

<p>The course runs online via Zoom. Your joining details are below. Please join a few minutes early
so we can start on time. Your trainer will be available after the session each day if you have
questions or want to chat further.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before Day 1 — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

-- ============================================================
-- EBM — Evidence-Based Management (1-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'EBM',
    'f2f',
    'Evidence-Based Management Training ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Date:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Evidence-Based Management class.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before the Day — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'EBM',
    'virtual',
    'Evidence-Based Management Training ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Date:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Evidence-Based Management class.</p>

<p>The course runs online via Zoom. Your joining details are below. Please join a few minutes early
so we can start on time.</p>

<p><strong>Zoom Link:</strong> <a href="{{zoom_url}}">{{zoom_url}}</a><br>
<strong>Meeting ID:</strong> {{zoom_id}}<br>
<strong>Passcode:</strong> {{zoom_passcode}}</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Before the Day — Self-study</strong></p>
{{self_study}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p><strong>Course Agenda</strong></p>
{{agenda}}

<hr style="border: none; border-top: 1px solid #ddd; margin: 24px 0;">

<p>See you soon,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type, format) DO NOTHING;
