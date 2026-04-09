-- V47: Redesign all pre-course email templates
-- Changes from V42/V44:
--   • Removed inner <!DOCTYPE>/<html>/<body> wrappers — EmailTemplateWrapper.Wrap() provides the shell
--   • Use {{course_full_name}} for heading (e.g. "Professional Scrum Master") instead of {{course_name}} ("PSM - Frazer-Nash (Bristol)")
--   • info-box div for course details (styled via wrapper CSS)
--   • <h2> section headers (styled with orange bottom border via wrapper CSS)
--   • b-agile branding (not b-agile)
--   • DO UPDATE so existing templates in production are refreshed
--
-- Available variables:
--   {{course_full_name}}  — human-readable course name  (e.g. "Professional Scrum Master")
--   {{course_name}}       — WooCommerce title            (e.g. "PSM - Frazer-Nash (Bristol)")
--   {{client_name}}       — client org or course code
--   {{dates}}             — formatted date range
--   {{times}}             — session times (default 09:00–17:00)
--   {{trainer_name}}      — trainer display name
--   {{venue_address}}     — F2F venue (f2f only)
--   {{zoom_url}}          — Zoom join link (virtual only)
--   {{zoom_id}}           — Zoom meeting ID (virtual only)
--   {{zoom_passcode}}     — Zoom passcode (virtual only)
--   {{self_study}}        — pre-reading list HTML (built per course type in C#)
--   {{agenda}}            — day-by-day agenda HTML (built per course type in C#)

-- ============================================================
-- PSM — Professional Scrum Master (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSM', 'f2f',
    'Your Professional Scrum Master Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSM', 'virtual',
    'Your Professional Scrum Master Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>The course runs online via Zoom. Please join a few minutes early so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- PSPO — Professional Scrum Product Owner (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSPO', 'f2f',
    'Your Professional Scrum Product Owner Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSPO', 'virtual',
    'Your Professional Scrum Product Owner Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>The course runs online via Zoom. Please join a few minutes early so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- PSK — Professional Scrum with Kanban (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSK', 'f2f',
    'Your Professional Scrum with Kanban Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSK', 'virtual',
    'Your Professional Scrum with Kanban Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>The course runs online via Zoom. Please join a few minutes early so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- APS-SD — Applying Professional Scrum for Software Development (3-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'APS-SD', 'f2f',
    'Your Applying Professional Scrum for Software Development Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>This is a 3-day hands-on course. Please aim to arrive around 10 minutes before the start on Day 1.
Bring your laptop if you plan to code during exercises — no specific language is required.
Your trainer will be available after each session if you have questions.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'APS-SD', 'virtual',
    'Your Applying Professional Scrum for Software Development Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>This is a 3-day hands-on course running online via Zoom. Please join a few minutes early on Day 1.
Bring your laptop if you plan to code during exercises — no specific language is required.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- PAL-E — Professional Agile Leadership Essentials (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PAL-E', 'f2f',
    'Your Professional Agile Leadership Essentials Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PAL-E', 'virtual',
    'Your Professional Agile Leadership Essentials Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>The course runs online via Zoom. Please join a few minutes early so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- PSPO-A — Professional Scrum Product Owner Advanced (1-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSPO-A', 'f2f',
    'Your Professional Scrum Product Owner Advanced Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Date</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>This advanced 1-day course builds on PSPO foundations — we will go deep into product strategy
and stakeholder complexity. Please aim to arrive around 10 minutes before the start.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before the Day — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSPO-A', 'virtual',
    'Your Professional Scrum Product Owner Advanced Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Date</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>This advanced 1-day course runs online via Zoom. It builds on PSPO foundations — we will go
deep into product strategy and stakeholder complexity. Please join a few minutes early.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before the Day — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- PSM-A — Professional Scrum Master Advanced (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSM-A', 'f2f',
    'Your Professional Scrum Master Advanced Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>This advanced course is designed for experienced Scrum Masters. We will work through complex
scenarios and coaching challenges. Please aim to arrive around 10 minutes before the start.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSM-A', 'virtual',
    'Your Professional Scrum Master Advanced Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>This advanced course for experienced Scrum Masters runs online via Zoom. We will work through
complex scenarios and coaching challenges. Please join a few minutes early.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- PSFS — Professional Scrum Facilitation Skills (1-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSFS', 'f2f',
    'Your Professional Scrum Facilitation Skills Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Date</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>This is a practical 1-day course with lots of exercises. Please aim to arrive around 10 minutes
before the start so we can get settled and begin on time.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before the Day — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSFS', 'virtual',
    'Your Professional Scrum Facilitation Skills Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Date</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>This is a practical 1-day course with lots of exercises, running online via Zoom.
Please join a few minutes early so we can start on time.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before the Day — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- PSU — Professional Scrum with User Experience (2-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSU', 'f2f',
    'Your Professional Scrum with User Experience Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'PSU', 'virtual',
    'Your Professional Scrum with User Experience Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Dates</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>The course runs online via Zoom. Please join a few minutes early so we can get settled and begin on time.
Your trainer will be available after the session each day if you have questions or want to chat further.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before Day 1 — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

-- ============================================================
-- EBM — Evidence-Based Management (1-day)
-- ============================================================

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'EBM', 'f2f',
    'Your Evidence-Based Management Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Date</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Venue</strong>&ensp;{{venue_address}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>Please aim to arrive around 10 minutes before the start so we can get settled and begin on time.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before the Day — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;

INSERT INTO bagile.pre_course_templates (course_type, format, subject_template, html_body) VALUES (
    'EBM', 'virtual',
    'Your Evidence-Based Management Training — {{dates}}',
    '<h2>{{course_full_name}}</h2>

<div class="info-box">
<strong>Date</strong>&ensp;{{dates}}<br>
<strong>Times</strong>&ensp;{{times}}<br>
<strong>Zoom</strong>&ensp;<a href="{{zoom_url}}">Join meeting</a><br>
<strong>Meeting ID</strong>&ensp;{{zoom_id}}<br>
<strong>Passcode</strong>&ensp;{{zoom_passcode}}
</div>

<p>Welcome to your Scrum.org {{course_full_name}} class with b-agile.</p>

<p>The course runs online via Zoom. Please join a few minutes early so we can start on time.</p>

<p>Any questions before the course? Drop us a line at <a href="mailto:info@bagile.co.uk">info@bagile.co.uk</a>.</p>

<h2>Before the Day — Self-study</h2>
{{self_study}}

<h2>Course Agenda</h2>
{{agenda}}

<p>See you soon,<br>
{{trainer_name}}<br>
b-agile</p>'
) ON CONFLICT (course_type, format) DO UPDATE
    SET subject_template = EXCLUDED.subject_template,
        html_body        = EXCLUDED.html_body;
