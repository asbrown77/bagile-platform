-- V42: Pre-course email templates per course type and format
-- Sprint 21: "Send Joining Details" compose flow
--
-- Separate from post_course_templates because structure and variables differ:
-- pre-course needs venue/zoom details and agenda; post-course needs resources/links.
-- Keyed by (course_type, format) so PSM F2F and PSM Virtual have different templates.

CREATE TABLE IF NOT EXISTS bagile.pre_course_templates (
    id               SERIAL PRIMARY KEY,
    course_type      VARCHAR(20)  NOT NULL,
    format           VARCHAR(20)  NOT NULL DEFAULT 'virtual',  -- 'virtual' or 'f2f'
    subject_template VARCHAR(500) NOT NULL,
    html_body        TEXT         NOT NULL,
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE(course_type, format)
);

COMMENT ON TABLE bagile.pre_course_templates IS
    'Pre-course joining information email templates, keyed by (course_type, format). '
    'Variables: {{course_name}}, {{dates}}, {{times}}, {{trainer_name}}, '
    '{{venue_address}} (f2f), {{zoom_url}}/{{zoom_id}}/{{zoom_passcode}} (virtual), '
    '{{client_name}}, {{self_study}}, {{agenda}}.';

-- ────────────────────────────────────────────────────────────────────────────
-- Seed: PSM F2F template (based on NHS Wales pre-course email)
-- ────────────────────────────────────────────────────────────────────────────

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSM',
    'f2f',
    'Professional Scrum Master Training ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}<br>
<strong>Venue:</strong> {{venue_address}}</p>

<p>Welcome to your Scrum.org Professional Scrum Master class.</p>

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

-- ────────────────────────────────────────────────────────────────────────────
-- Seed: PSM Virtual template (Zoom details instead of venue address)
-- ────────────────────────────────────────────────────────────────────────────

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSM',
    'virtual',
    'Professional Scrum Master Training ({{client_name}}) - {{dates}}',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p style="font-size: 18px; font-weight: bold; color: #1a2e5e;">{{course_name}}</p>
<p><strong>Dates:</strong> {{dates}}<br>
<strong>Times:</strong> {{times}}</p>

<p>Welcome to your Scrum.org Professional Scrum Master class.</p>

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
