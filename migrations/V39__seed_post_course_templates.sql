-- V39: Seed post-course email templates for all remaining course types
-- Sprint 18: templates for PSM, PSPO-A, PSU, PAL-EBM, plus placeholders for AI/other variants
-- Single quotes inside strings are doubled ('') per SQL standard, matching V37 pattern.

-- ============================================================
-- PSM
-- ============================================================
INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSM',
    'Your PSM resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Scrum Master course {{course_dates}}. It was a fantastic group and we really enjoyed the two days with you.</p>

{{delay_note}}

<p>As promised, here are your resources and next steps:</p>

<h3>&#128203; Your PSM I Assessment</h3>
<ul>
  <li>You have a free attempt included with your course &mdash; check your scrum.org account for the password.</li>
  <li>The PSM I is 80 questions, 60 minutes, and you need <strong>85%</strong> to pass.</li>
  <li><a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open assessment</a> &mdash; warm-up practice, aim for 95%+ before attempting the real thing</li>
  <li><a href="https://www.scrum.org/open-assessments/product-owner-open">Product Owner Open assessment</a> &mdash; good for testing breadth of Scrum knowledge</li>
</ul>

<h3>&#127891; Study Tips</h3>
<ul>
  <li>Focus areas: <em>Understanding and Applying the Scrum Framework</em> and <em>Developing People and Teams</em></li>
  <li>Review the <a href="https://www.scrum.org/pathway/scrum-master/">Scrum Master learning path</a> on scrum.org</li>
  <li>Re-read the Scrum Guide carefully &mdash; many questions are based on precise wording</li>
</ul>

<h3>&#128161; Helpful Resources</h3>
<ul>
  <li><a href="https://www.scrum.org/resources/blog/tips-passing-psm-i-assessment">Tips for passing the PSM I</a> &mdash; scrum.org blog</li>
  <li><a href="https://github.com/dlresende/tech-debt-simulator">Tech Debt Simulator</a> &mdash; great for understanding technical debt impact</li>
  <li><a href="https://retromat.org">Retromat</a> &mdash; retrospective formats and ideas</li>
  <li><a href="https://www.planningpoker.com">Planning Poker</a> &mdash; online estimation tool</li>
  <li><a href="https://www.scrum.org/resources/8-stances-scrum-master">The 8 Stances of a Scrum Master</a> &mdash; Gunther Verheyen</li>
  <li><a href="https://www.liberatingstructures.com">Liberating Structures</a> &mdash; facilitation techniques for Scrum events</li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<ul>
  <li><em>Scrum: A Pocket Guide</em> &mdash; Gunther Verheyen (best concise Scrum reference)</li>
  <li><em>The 8 Stances of a Scrum Master</em> &mdash; Barry Overeem &amp; Christiaan Verwijs</li>
  <li><em>Practices for Scaling Lean &amp; Agile Development</em> &mdash; Craig Larman (pair programming, mob programming)</li>
</ul>

<h3>&#129309; Stay Connected</h3>
<p>If you have any questions as you prepare for your assessment, drop us an email and we''ll help where we can. We''d also love to hear how you get on &mdash; let us know when you pass!</p>

<h3>&#128172; Feedback</h3>
<p>We''d really appreciate a quick review on <a href="https://www.scrum.org/find-trainers/chris-bexon">Chris''s scrum.org profile</a> if you enjoyed the course. It helps others find us.</p>

<p>Good luck!</p>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

-- ============================================================
-- PSPO-A (Advanced)
-- ============================================================
INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSPO-A',
    'Your PSPO-A resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Scrum Product Owner Advanced course {{course_dates}}. It was a pleasure working through the more complex product challenges with you.</p>

{{delay_note}}

<p>Here are your resources and next steps:</p>

<h3>&#128203; Your PSPO II Assessment</h3>
<ul>
  <li>You have a free attempt included with your course &mdash; check your scrum.org account for the password.</li>
  <li>The PSPO II is scenario-based, so there are no clear right/wrong answers &mdash; focus on understanding the reasoning behind decisions rather than memorising facts.</li>
  <li><a href="https://www.scrum.org/open-assessments/product-owner-open">Product Owner Open assessment</a> &mdash; good warm-up, but note the PSPO II goes much deeper into real-world scenarios</li>
</ul>

<h3>&#127891; Study Tips</h3>
<ul>
  <li>Review the <a href="https://www.scrum.org/pathway/product-owner/">Product Owner learning path</a> on scrum.org</li>
  <li>Study the <a href="https://www.scrum.org/resources/scrum-guide">Scrum Guide</a> and <a href="https://www.scrum.org/resources/evidence-based-management-guide">EBM Guide</a> thoroughly</li>
  <li>Think about outcomes over outputs &mdash; the PSPO II tests whether you can prioritise for real business value</li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<ul>
  <li><em>Impact Mapping</em> &mdash; Gojko Adzic</li>
  <li><em>User Story Mapping</em> &mdash; Jeff Patton</li>
  <li><em>Lean UX</em> &mdash; Jeff Gothelf &amp; Josh Seiden</li>
  <li><em>Lean Enterprise</em> &mdash; Jez Humble, Joanne Molesky &amp; Barry O''Reilly</li>
  <li><em>Beyond Value Stream Mapping (BVSSH)</em> &mdash; Jonathan Smart</li>
  <li><em>Good to Great</em> &mdash; Jim Collins</li>
  <li><em>Extreme Ownership</em> &mdash; Jocko Willink &amp; Leif Babin</li>
  <li><em>Unlocking Business Agility with Evidence-Based Management</em> &mdash; Patricia Kong &amp; Todd Miller</li>
  <li><em>The Lean Startup</em> &mdash; Eric Ries</li>
</ul>

<h3>&#129309; Stay Connected</h3>
<p>If you have questions as you prepare for the assessment, drop us a message. We''re happy to help.</p>

<h3>&#128172; Feedback</h3>
<p>A quick review on <a href="https://www.scrum.org/find-trainers/chris-bexon">scrum.org</a> is always appreciated if you found the course valuable.</p>

<p>Good luck!</p>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

-- ============================================================
-- PSU (Professional Scrum with UX)
-- ============================================================
INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSU',
    'Your PSU resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>Thank you for joining us on the Professional Scrum with UX course {{course_dates}}. It was great exploring the intersection of Scrum and UX design thinking with you.</p>

{{delay_note}}

<p>Here are your resources and next steps:</p>

<h3>&#128203; Your PSU Assessment</h3>
<ul>
  <li>You have a free attempt included with your course &mdash; check your scrum.org account for the password.</li>
  <li><a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open assessment</a> &mdash; good warm-up for the Scrum fundamentals portion</li>
  <li>Review the UX concepts from the course slides, particularly Design Thinking integration with the Sprint cycle</li>
</ul>

<h3>&#127891; Study Tips</h3>
<ul>
  <li>Review the <a href="https://www.scrum.org/pathway/product-owner/">Product Owner learning path</a> &mdash; PSU overlaps heavily with PO responsibilities</li>
  <li>Re-read the <a href="https://www.scrum.org/resources/scrum-guide">Scrum Guide</a> with UX in mind &mdash; where does research fit within Sprints?</li>
</ul>

<h3>&#128161; Helpful Resources</h3>
<ul>
  <li><a href="https://www.nngroup.com/articles/truth-curve/">The Truth Curve</a> &mdash; Nielsen Norman Group, on discovering truth early</li>
  <li><a href="https://www.scrum.org/resources/blog/user-research-sprints">User Research in Sprints</a> &mdash; scrum.org blog</li>
  <li><a href="https://www.youtube.com/watch?v=szr0ezLyQHY">Nordstrom Innovation Lab</a> &mdash; Lean UX in action (video)</li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<ul>
  <li><em>Lean UX</em> &mdash; Jeff Gothelf &amp; Josh Seiden</li>
  <li><em>User Story Mapping</em> &mdash; Jeff Patton</li>
  <li><em>Continuous Discovery Habits</em> &mdash; Teresa Torres</li>
</ul>

<h3>&#129309; Stay Connected</h3>
<p>Any questions as you prepare for the assessment, feel free to get in touch.</p>

<h3>&#128172; Feedback</h3>
<p>A review on <a href="https://www.scrum.org/find-trainers/chris-bexon">scrum.org</a> is always appreciated.</p>

<p>Good luck!</p>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

-- ============================================================
-- PAL-EBM (Professional Agile Leadership - EBM)
-- ============================================================
INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PAL-EBM',
    'Your PAL-EBM resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Agile Leadership course {{course_dates}}. The conversations around leadership and Evidence-Based Management were really insightful &mdash; thanks for bringing your experience to the room.</p>

{{delay_note}}

<p>Here are your resources and next steps:</p>

<h3>&#128203; Your PAL-EBM Assessment</h3>
<ul>
  <li>You have a free attempt included with your course &mdash; check your scrum.org account for the password.</li>
  <li><a href="https://www.scrum.org/open-assessments/ebm-open">EBM Open assessment</a> &mdash; use this to test your understanding before the real attempt</li>
</ul>

<h3>&#127891; Study Tips</h3>
<ul>
  <li>Re-read the <a href="https://www.scrum.org/resources/evidence-based-management-guide">EBM Guide</a> &mdash; the four Key Value Areas and Key Value Measures are frequently tested</li>
  <li>Think about how EBM applies in your own organisation &mdash; the assessment uses real leadership scenarios</li>
</ul>

<h3>&#128161; Helpful Resources</h3>
<ul>
  <li><a href="https://www.scrum.org/resources/evidence-based-management-guide">EBM Guide</a> &mdash; the core reference document</li>
  <li><a href="https://www.scrum.org/resources/product-goal-canvas">Product Goal Canvas</a> &mdash; useful tool for defining outcomes</li>
  <li><a href="https://www.whatmatters.com/articles/okr-meaning-definition-example/">OKRs explained</a> &mdash; how OKRs relate to EBM goals</li>
  <li><a href="https://www.youtube.com/watch?v=OqmdLcyES_Q">Turn the Ship Around</a> &mdash; David Marquet on leader-leader vs leader-follower (video)</li>
  <li><a href="https://www.whatmatters.com">What Matters (OKRs)</a> &mdash; John Doerr''s OKR resource site</li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<ul>
  <li><em>Unlocking Business Agility with Evidence-Based Management</em> &mdash; Patricia Kong &amp; Todd Miller</li>
  <li><em>Turn the Ship Around</em> &mdash; David Marquet</li>
  <li><em>Measure What Matters</em> &mdash; John Doerr (OKRs)</li>
  <li><em>Beyond Value Stream Mapping (BVSSH)</em> &mdash; Jonathan Smart</li>
</ul>

<h3>&#129309; Stay Connected</h3>
<p>If you have questions as you prepare for the assessment, feel free to reach out. We''d love to hear how you apply EBM in your organisation.</p>

<h3>&#128172; Feedback</h3>
<p>A review on <a href="https://www.scrum.org/find-trainers/chris-bexon">scrum.org</a> is always appreciated and helps other leaders find the course.</p>

<p>Good luck!</p>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

-- ============================================================
-- Placeholder templates — need customising
-- Based on closest parent: PSM-AI/PSPO-AI from PSPO, PSK/PAL-E from PSM,
-- PSM-A/PSFS/APS-SD are specialised variants
-- ============================================================

INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSM-AI',
    'Your PSM-AI resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Scrum Master Applied AI course {{course_dates}}.</p>

{{delay_note}}

<p><em>Note: This template needs customising with PSM-AI specific resources, assessment information, and recommended reading. Based on the PSM template as a starting point.</em></p>

<p>In the meantime, here are the core Scrum resources:</p>

<ul>
  <li><a href="https://www.scrum.org/resources/scrum-guide">The Scrum Guide</a></li>
  <li><a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open assessment</a></li>
</ul>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSPO-AI',
    'Your PSPO-AI resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Scrum Product Owner Applied AI course {{course_dates}}.</p>

{{delay_note}}

<p><em>Note: This template needs customising with PSPO-AI specific resources, assessment information, and recommended reading. Based on the PSPO template as a starting point.</em></p>

<ul>
  <li><a href="https://www.scrum.org/resources/scrum-guide">The Scrum Guide</a></li>
  <li><a href="https://www.scrum.org/open-assessments/product-owner-open">Product Owner Open assessment</a></li>
</ul>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSK',
    'Your PSK resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Scrum with Kanban course {{course_dates}}.</p>

{{delay_note}}

<p><em>Note: This template needs customising with PSK-specific resources, the Kanban Guide for Scrum Teams, assessment information, and recommended reading.</em></p>

<ul>
  <li><a href="https://www.scrum.org/resources/scrum-guide">The Scrum Guide</a></li>
  <li><a href="https://www.scrum.org/resources/kanban-guide-scrum-teams">Kanban Guide for Scrum Teams</a></li>
  <li><a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open assessment</a></li>
</ul>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PAL-E',
    'Your PAL-E resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Agile Leadership Essentials course {{course_dates}}.</p>

{{delay_note}}

<p><em>Note: This template needs customising with PAL-E specific resources, assessment information, and recommended reading. Based on the PAL-EBM template as a starting point.</em></p>

<ul>
  <li><a href="https://www.scrum.org/resources/scrum-guide">The Scrum Guide</a></li>
  <li><a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open assessment</a></li>
</ul>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSM-A',
    'Your PSM-A resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Scrum Master Advanced course {{course_dates}}.</p>

{{delay_note}}

<p><em>Note: This template needs customising with PSM-A specific resources, PSM II assessment information (scenario-based), and recommended reading.</em></p>

<ul>
  <li><a href="https://www.scrum.org/resources/scrum-guide">The Scrum Guide</a></li>
  <li><a href="https://www.scrum.org/pathway/scrum-master/">Scrum Master learning path</a></li>
</ul>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'PSFS',
    'Your PSFS resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Professional Scrum Facilitation Skills course {{course_dates}}.</p>

{{delay_note}}

<p><em>Note: This template needs customising with PSFS-specific resources, facilitation techniques, assessment information, and recommended reading.</em></p>

<ul>
  <li><a href="https://www.scrum.org/resources/scrum-guide">The Scrum Guide</a></li>
  <li><a href="https://www.liberatingstructures.com">Liberating Structures</a></li>
</ul>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;

INSERT INTO bagile.post_course_templates (course_type, subject_template, html_body) VALUES (
    'APS-SD',
    'Your APS-SD resources and next steps',
    '<!DOCTYPE html>
<html>
<body style="font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; color: #333;">

<p>{{greeting}}</p>

<p>Thank you for joining us on the Applying Professional Scrum for Software Development course {{course_dates}}.</p>

{{delay_note}}

<p><em>Note: This template needs customising with APS-SD specific resources, technical practices (TDD, CI/CD, pair programming, DevOps), assessment information, and recommended reading.</em></p>

<ul>
  <li><a href="https://www.scrum.org/resources/scrum-guide">The Scrum Guide</a></li>
  <li><a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open assessment</a></li>
</ul>

<p>Cheers,<br>{{trainer_name}}<br>b-agile</p>

</body>
</html>'
) ON CONFLICT (course_type) DO NOTHING;
