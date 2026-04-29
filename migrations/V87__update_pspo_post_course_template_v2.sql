-- V87: Update PSPO post-course follow-up template to v2
--
-- Source: Alex Brown's actual sent PSPO follow-up email from 3 Feb 2026
-- (subject "PSPO Training Follow-Up: Next Steps & Resources to Help You
-- Succeed", thread 19d48f703d02a7eb in alexbrown@bagile.co.uk sent folder).
--
-- Three universal additions on top of Alex's content:
--   1. PSPO II 40% assessment discount mention (path to PSPO-A)
--   2. Scrum.org profile My Classes association point (records attendance
--      regardless of pass/fail)
--   3. {{trainer_name}} sign-off so it works for any trainer
--
-- Style rules (same as V86):
--   - No em-dashes. Use full stops, commas or regular hyphens.
--   - Body uses HTML entity &rsquo; for apostrophes (clean SQL escaping).
--   - Body omits DOCTYPE/html/body and contact footer (EmailTemplateWrapper
--     supplies the branded shell).
--   - Practice tests are Scrum.org-official only (no third-party tools).

UPDATE bagile.post_course_templates
SET subject_template = 'PSPO Training Follow-Up: Next Steps & Resources to Help You Succeed',
    html_body        = '<p>{{greeting}}</p>

<p>It was an absolute pleasure spending two days with such a talented and motivated group on the Professional Scrum Product Owner course {{course_dates}}. Thank you for bringing the course to life and contributing to the discussions.</p>

{{delay_note}}

<h3>&#129309; Stay Connected</h3>
<p>Thank you for choosing us as your training partner. We&rsquo;d love to stay in touch:</p>
<ul>
  <li>Connect with me on <a href="https://www.linkedin.com/in/ukalexbrown">LinkedIn</a></li>
  <li>Follow <a href="https://www.linkedin.com/company/bagile">b-agile on LinkedIn</a></li>
</ul>

<h3>&#128205; Your Next Steps</h3>
<p>Your class will be registered with Scrum.org today. Scrum.org is based in Chicago, so you should receive your PSPO assessment password by this evening UK time. If you don&rsquo;t see it by tomorrow morning, please let me know.</p>
<p>Once registered, this course will also show on your Scrum.org profile under <strong>My Classes</strong> with the class name and trainer, regardless of whether you sit the assessment.</p>

<h3>&#128203; Assessment Information</h3>
<ul>
  <li>You can get a free retake if you take the assessment within 14 days and don&rsquo;t pass at 85%. The 14 days start once Scrum.org registers you.</li>
  <li>There&rsquo;s no expiry on the assessment key, so take it whenever you&rsquo;re ready.</li>
  <li>As a PSPO I student, you also qualify for a 40% discount on the PSPO II assessment if you want to push on to the advanced level.</li>
</ul>

<h3>&#127891; Study Tips to Help You Pass</h3>
<p>Please review the <a href="https://www.scrum.org/pathway/product-owner/">Product Owner learning path</a>. The two areas worth focusing on are Understanding and Applying the Scrum Framework, and Managing Products with Agility.</p>

<p>Preparation made a huge difference for many past students, even after attending the class:</p>
<ul>
  <li>Read the <a href="https://www.scrum.org/resources/scrum-guide">Scrum Guide</a> thoroughly. Make sure you know the accountabilities, artifacts and their commitments, and the events.</li>
  <li>Take the <a href="https://www.scrum.org/open-assessments/product-owner-open">Product Owner Open Assessment</a> for practice. Aim to score 100% three times in a row before sitting PSPO I.</li>
  <li>Take the <a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open Assessment</a> for broader Scrum perspective.</li>
</ul>

<h3>&#128161; Helpful Resources</h3>
<p>All course materials, including the student reference booklet, are available in your Scrum.org profile. Go to <strong>My Classes</strong> and click <strong>Go to Classroom</strong> next to this course.</p>

<p>Useful B-Agile blogs:</p>
<ul>
  <li><a href="https://www.bagile.co.uk/tips-to-pass-the-pspo-i-assessment/">10 Powerful Tips to Pass the PSPO I Assessment</a></li>
  <li><a href="https://www.bagile.co.uk/value-stream-mapping-your-answer/">Where to Start with Scrum? Is Value Stream Mapping Your Answer?</a></li>
  <li><a href="https://www.bagile.co.uk/definition-of-done-where-to-start/">Definition of Done. Where to Start?</a></li>
  <li><a href="https://www.bagile.co.uk/technical-debt-silent-threat/">Technical Debt: Understanding and Overcoming the Silent Threat</a></li>
  <li><a href="https://www.bagile.co.uk/to-much-stuff-in-your-product-backlog/">Too much stuff in your Product Backlog?</a></li>
</ul>

<p>Impact Mapping:</p>
<ul>
  <li><a href="https://www.impactmapping.org/about.html">Impact Mapping</a> (site)</li>
  <li><a href="https://www.youtube.com/watch?v=yQzIfKzU9PI">Impact Mapping and Story Mapping</a> (video)</li>
  <li><a href="https://www.youtube.com/watch?v=govutBfXPQ">Gojko Adzic, Fast-track from Idea to Impact</a> (video)</li>
</ul>

<p>Story Mapping:</p>
<ul>
  <li><a href="https://www.jpattonassociates.com/story-mapping-quick-ref/">Story Mapping Quick Reference</a> (cheat sheet)</li>
  <li><a href="https://www.youtube.com/watch?v=mqBGO0wgNQM">The Game Has Changed</a> by Jeff Patton</li>
  <li><a href="https://jpattonassociates.com/user-story-mapping-presentation/">User Story Mapping</a> by Jeff Patton</li>
</ul>

<p>Wider Product Owner practice:</p>
<ul>
  <li><a href="https://effectiveagile.com/canvases/product-goal-canvas/">Product Goal Canvas</a></li>
  <li><a href="https://www.liberatingstructures.com/">Liberating Structures</a></li>
  <li><a href="https://www.scrum.org/resources/evidence-based-management">Evidence-Based Management (EBM)</a></li>
  <li><a href="https://medium.com/serious-scrum/how-to-empower-your-safe-pos-to-become-true-product-owners-af3f29a480ae">How to Empower Your SAFe POs to Become True Product Owners</a></li>
  <li><a href="https://www.ted.com/talks/simon_sinek_how_great_leaders_inspire_action">Simon Sinek, How Great Leaders Inspire Action</a> (TED Talk)</li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<p>To deepen your understanding, here are a few books I mentioned during the course:</p>
<ul>
  <li><a href="https://amzn.to/46TzYEd">Better Value Sooner Safer Happier</a> by Jon Smart</li>
  <li><a href="https://amzn.to/4akRNig">Specification by Example</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/3Nu6khU">Impact Mapping</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/3Tto7cL">User Story Mapping</a> by Jeff Patton</li>
  <li><a href="https://amzn.to/3M4oO80">Lean UX</a> by Jeff Gothelf and Josh Seiden</li>
  <li><a href="https://amzn.to/4903L0i">Lean Enterprise: How High Performance Organizations Innovate at Scale</a></li>
  <li><a href="https://amzn.to/3uXI2qf">Fifty Quick Ideas To Improve Your User Stories</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/3RJ8Mnl">Zombie Scrum Survival Guide</a> by Barry Overeem and Christiaan Verwijs</li>
  <li><a href="https://amzn.to/3M4pEkV">User Stories Applied</a> by Mike Cohn</li>
  <li><a href="https://amzn.to/41jzKVL">Agile Estimating and Planning</a> by Mike Cohn</li>
</ul>

<h3>&#11088; We&rsquo;d Love Your Review</h3>
<p>Please share your feedback through the <strong>Review the Class</strong> button on your Scrum.org profile. It takes less than five minutes and is the single biggest help in letting future students decide whether we&rsquo;re the right trainers for them. Feel free to mention us in any posts about the class or your certification journey, we&rsquo;d love to celebrate with you.</p>

<h3>&#128640; What&rsquo;s Next for You</h3>
<ul>
  <li><strong>PSPO-A (advanced):</strong> Our PSPO-A course leads to PSPO II. With your 40% PSPO II assessment discount, this is the strongest next step.</li>
  <li><strong>PSPO-AI Essentials:</strong> A one-day course on using AI as a Product Owner.</li>
  <li><strong>Public Courses:</strong> Browse our <a href="https://www.bagile.co.uk/our-courses/">upcoming courses</a>. Returning customers get 10% off with code <strong>RETURN10</strong>.</li>
  <li><strong>Private Training:</strong> See our <a href="https://www.bagile.co.uk/private-agile-training/">Private Training</a> page for in-house options.</li>
  <li><strong>Coaching &amp; Consulting:</strong> Need agile coaching or consulting? Drop us a line at info@bagile.co.uk.</li>
</ul>

<p>Wishing you success in your assessment and beyond. If you have any questions, feel free to reach out.</p>

<p><strong>Scrum On!</strong><br>
{{trainer_name}}</p>',
    updated_at = NOW()
WHERE course_type = 'PSPO';
