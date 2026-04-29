-- V86: Update PSM post-course follow-up template
--
-- Updates the V39 thin seeded PSM template to match what trainers have been
-- sending manually from Gmail (preserving Alex's voice and section structure)
-- with three high-value additions:
--   1. Mikhail Lapshin's free PSM I mocks (community gold standard)
--   2. PSM II 40% assessment discount mention (path to PSM-A)
--   3. Scrum.org profile My Classes association point (records attendance
--      regardless of pass/fail)
--
-- Also corrects two issues from V39:
--   - Wrong PAL-EBM URL stacked on the Scrum Master learning path link
--   - Subject changed from "Your PSM resources and next steps" to
--     "PSM Training Follow-Up: Next Steps & Resources to Help You Succeed"
--     to match what trainers have been sending and read more warmly
--
-- Removed dead URLs (DNS no longer resolves):
--   - tastycupcakes.org
--   - collaboration.csc.ncsu.edu (NCSU pair programming paper)
--
-- Style rules:
--   - Em-dashes avoided. Use full stops, commas or regular hyphens.
--   - Body uses HTML entity for apostrophes to keep SQL escaping clean.
--   - Body intentionally omits DOCTYPE/html/body tags and the contact
--     footer. EmailTemplateWrapper.Wrap() supplies the branded shell.
--   - Sign-off uses {{trainer_name}} so the template works for both Alex
--     and Chris regardless of who delivered the class.

UPDATE bagile.post_course_templates
SET subject_template = 'PSM Training Follow-Up: Next Steps & Resources to Help You Succeed',
    html_body        = '<p>{{greeting}}</p>

<p>It was an absolute pleasure spending two days with such a talented and motivated group on the Professional Scrum Master course {{course_dates}}. Thank you for your enthusiasm, insightful questions, and engaging discussions that brought the course to life.</p>

{{delay_note}}

<h3>&#129309; Stay Connected</h3>
<p>Thank you for choosing us as your training partner. We&rsquo;d love to stay in touch:</p>
<ul>
  <li>Connect with me on <a href="https://www.linkedin.com/in/ukalexbrown">LinkedIn</a></li>
  <li>Follow <a href="https://www.linkedin.com/company/bagile">b-agile on LinkedIn</a></li>
</ul>

<h3>&#128205; Your Next Steps</h3>
<p>Your class will be registered with Scrum.org today. Scrum.org is based in Chicago, so you should receive your PSM assessment password by this evening UK time. If you don&rsquo;t see it by tomorrow morning, please let me know.</p>
<p>Once registered, this course will also show on your Scrum.org profile under <strong>My Classes</strong> with the class name and trainer, regardless of whether you sit the assessment.</p>

<h3>&#128203; Assessment Information</h3>
<ul>
  <li>The PSM I assessment is <strong>80 multiple-choice questions, 60 minutes, 85% to pass</strong>.</li>
  <li>You get two attempts: a first attempt now, and a free retake automatically issued if you sit it within 14 days and score under 85%.</li>
  <li>The password has no expiry, so take it when you&rsquo;re ready.</li>
  <li>As a PSM I student, you also qualify for a 40% discount on the PSM II assessment if you want to push on to the advanced level.</li>
</ul>

<h3>&#127891; Study Tips to Help You Pass</h3>
<p>Please review the <a href="https://www.scrum.org/pathway/scrum-master/">Scrum Master learning path</a>. The two areas worth focusing on are Understanding and Applying the Scrum Framework, and Developing People and Teams.</p>

<p>Preparation made a huge difference for many past students, even after attending the class:</p>
<ul>
  <li>Read the <a href="https://www.scrum.org/resources/scrum-guide">Scrum Guide</a> thoroughly. Make sure you know the accountabilities, artifacts and their commitments, and the events.</li>
  <li>Take the <a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open Assessment</a> for practice. Aim to score 100% three times in a row before sitting PSM I.</li>
  <li>Take the <a href="https://www.scrum.org/open-assessments/product-owner-open">Product Owner Open Assessment</a> for broader Scrum perspective.</li>
  <li>Try <a href="https://mlapshin.com/index.php/scrum-quizzes/">Mikhail Lapshin&rsquo;s free PSM I mocks</a>. Community gold standard, harder than the Scrum Open and great for stretching yourself.</li>
</ul>

<h3>&#128161; Helpful Resources</h3>
<p>All course materials, including the student reference booklet, are available in your Scrum.org profile. Go to <strong>My Classes</strong> and click <strong>Go to Classroom</strong> next to this course.</p>

<p>Some useful exercises and articles for your journey:</p>

<p>Useful B-Agile blogs:</p>
<ul>
  <li><a href="https://www.bagile.co.uk/tips-to-pass-the-psm-i-assessment/">10 Powerful Tips to Pass the PSM I Assessment</a></li>
  <li><a href="https://www.bagile.co.uk/what-is-scrum-uncovering-its-true-essence/">What is Scrum? Uncovering Its True Essence</a></li>
  <li><a href="https://www.bagile.co.uk/value-stream-mapping-your-answer/">Where to Start with Scrum? Is Value Stream Mapping Your Answer?</a></li>
  <li><a href="https://www.bagile.co.uk/definition-of-done-where-to-start/">Definition of Done. Where to Start?</a></li>
  <li><a href="https://www.bagile.co.uk/escaping-the-product-owners-trap/">Escaping the Product Owner&rsquo;s Trap</a></li>
  <li><a href="https://www.bagile.co.uk/agile-estimation-mindset/">Agile Estimation Isn&rsquo;t Broken. But the Thinking Might Be.</a></li>
</ul>

<p>Tools and exercises:</p>
<ul>
  <li><a href="https://www.bagile.co.uk/techdebt-simulator/">Tech Debt Simulator</a></li>
  <li><a href="https://retromat.org/en/">Retromat</a> for retrospective formats and ideas</li>
  <li><a href="https://www.pointingpoker.com/">Planning Poker</a></li>
  <li><a href="https://www.liberatingstructures.com/">Liberating Structures</a></li>
</ul>

<p>Story Mapping and Impact Mapping:</p>
<ul>
  <li><a href="https://www.youtube.com/watch?v=mqBGO0wgNQM">The Game Has Changed</a> by Jeff Patton</li>
  <li><a href="https://jpattonassociates.com/user-story-mapping-presentation/">User Story Mapping</a> by Jeff Patton</li>
  <li><a href="https://www.youtube.com/watch?v=yQzIfKzU9PI">Impact Mapping and Story Mapping</a></li>
  <li><a href="https://www.youtube.com/watch?v=govutBfXPQ">Gojko Adzic, Fast-track from Idea to Impact</a></li>
</ul>

<p>Wider Scrum Master practice:</p>
<ul>
  <li><a href="https://www.youtube.com/watch?v=SHOVVnRB4h0">Mob Programming: A Whole Team Approach</a></li>
  <li><a href="https://www.scrum.org/resources/8-stances-scrum-master">8 Stances of a Scrum Master</a></li>
  <li><a href="https://www.scrum.org/resources/evidence-based-management">Evidence-Based Management (EBM)</a></li>
  <li><a href="https://effectiveagile.com/canvases/product-goal-canvas/">Product Goal Canvas</a></li>
  <li><a href="https://www.scrum.org/resources/blog/day-life-scrum-master">A Day in the Life of a Scrum Master</a></li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<p>To deepen your understanding, here are a few books I mentioned during the course:</p>
<ul>
  <li><a href="https://amzn.to/3sGNhK5">Scrum: A Pocket Guide</a> by Gunther Verheyen</li>
  <li><a href="https://amzn.to/3sNGazt">Zombie Scrum Survival Guide</a> by Barry Overeem and Christiaan Verwijs</li>
  <li><a href="https://amzn.to/46pdMBw">Agile Retrospectives</a> by Esther Derby</li>
  <li><a href="https://amzn.to/3SsnbF8">Better Value Sooner Safer Happier</a> by Jon Smart</li>
  <li><a href="https://amzn.to/3QoMPYI">Good to Great</a> by Jim Collins</li>
  <li><a href="https://amzn.to/3MVXw3X">Agile Estimating and Planning</a> by Mike Cohn</li>
  <li><a href="https://amzn.to/3GaHyPG">Coaching Agile Teams</a> by Lyssa Adkins</li>
  <li><a href="https://amzn.to/3MSFEXJ">Extraordinary Badass Agile Coach</a> by Robert L. Galen</li>
  <li><a href="https://amzn.to/3uAdpXm">Scrum Mastery</a> by Geoff Watts</li>
  <li><a href="https://amzn.to/3Nu6khU">Impact Mapping</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/3Tto7cL">User Story Mapping</a> by Jeff Patton</li>
  <li><a href="https://amzn.to/3uXI2qf">Fifty Quick Ideas To Improve Your User Stories</a> by Gojko Adzic</li>
</ul>

<h3>&#11088; We&rsquo;d Love Your Review</h3>
<p>Please share your feedback through the <strong>Review the Class</strong> button on your Scrum.org profile. It takes less than five minutes and is the single biggest help in letting future students decide whether we&rsquo;re the right trainers for them. Feel free to mention us in any posts about the class or your certification journey, we&rsquo;d love to celebrate with you.</p>

<h3>&#128640; What&rsquo;s Next for You</h3>
<ul>
  <li><strong>PSM-A (advanced):</strong> Our PSM-A course leads to PSM II. With your 40% PSM II assessment discount, this is the strongest next step.</li>
  <li><strong>PSM-AI Essentials:</strong> A one-day course on using AI as a Scrum Master.</li>
  <li><strong>Public Courses:</strong> Browse our <a href="https://www.bagile.co.uk/our-courses/">upcoming courses</a>. Returning customers get 10% off with code <strong>RETURN10</strong>.</li>
  <li><strong>Private Training:</strong> See our <a href="https://www.bagile.co.uk/private-agile-training/">Private Training</a> page for in-house options.</li>
  <li><strong>Coaching &amp; Consulting:</strong> Need agile coaching or consulting? Drop us a line at info@bagile.co.uk.</li>
</ul>

<p>Wishing you success in your assessment and beyond. If you have any questions, feel free to reach out.</p>

<p><strong>Scrum On!</strong><br>
{{trainer_name}}</p>',
    updated_at = NOW()
WHERE course_type = 'PSM';
