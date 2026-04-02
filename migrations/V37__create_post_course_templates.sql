-- V37: Post-course email templates per course type
-- Sprint 16: one-click follow-up from portal

CREATE TABLE bagile.post_course_templates (
    id              SERIAL PRIMARY KEY,
    course_type     VARCHAR(20)  NOT NULL UNIQUE,  -- PSM, PSPO, PSPOA, PSU, PALEBM, etc.
    subject_template TEXT        NOT NULL,
    html_body       TEXT        NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Seed: PSPO template using placeholder variables
INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSPO',
    'Your PSPO resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you so much for joining us on the Professional Scrum Product Owner course {{course_dates}}. It was great to have you along.</p>

{{delay_note}}

<p>As promised, here are your resources and next steps:</p>

<p><strong>Your PSPO I Assessment</strong></p>
<ul>
  <li>You have a free attempt included with your course — check your scrum.org account for the password.</li>
  <li><a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open assessment</a> — warm-up practice, aim for 95%+ before attempting the real thing</li>
  <li><a href="https://www.scrum.org/open-assessments/product-owner-open">Product Owner Open assessment</a> — covers PSPO I topics directly</li>
</ul>

<p><strong>Recommended Reading</strong></p>
<ul>
  <li><em>Scrum: A Pocket Guide</em> — Gunther Verheyen (best concise Scrum reference)</li>
  <li><em>Continuous Discovery Habits</em> — Teresa Torres (Product Discovery)</li>
  <li><em>User Story Mapping</em> — Jeff Patton (Backlog structuring)</li>
  <li><em>The Lean Startup</em> — Eric Ries (Build-Measure-Learn)</li>
</ul>

<p><strong>Useful Links</strong></p>
<ul>
  <li><a href="https://www.scrum.org/resources/scrum-guide">The Scrum Guide</a></li>
  <li><a href="https://www.scrum.org/resources/nexus-guide">The Nexus Guide</a> (scaling)</li>
  <li><a href="https://www.bagile.co.uk/blog/">b-agile blog</a></li>
</ul>

<p>If you have any questions as you prepare for your assessment, drop us an email and we''ll help where we can.</p>

<p>Good luck!</p>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
);
