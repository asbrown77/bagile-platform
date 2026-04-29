-- V88: Update PSPO-A post-course follow-up template to v2
--
-- Source: Alex Brown's actual sent PSPO-A follow-up email from 14 Nov 2024
-- (subject "PSPO-A Training Follow-Up: Next Steps & Resources to Help You
-- Succeed", thread 1932a6bd978e2882 in alexbrown@bagile.co.uk sent folder).
--
-- Two universal additions on top of Alex's content:
--   1. Scrum.org profile My Classes association point
--   2. {{trainer_name}} sign-off
--
-- (40% next-level discount not added for PSPO-A since attendees are already
-- at the advanced level. PSM-style "next step is X-A" framing doesn't apply.)
--
-- Style rules: zero em-dashes, Scrum.org-official practice tests only.

UPDATE bagile.post_course_templates
SET subject_template = 'PSPO-A Training Follow-Up: Next Steps & Resources to Help You Succeed',
    html_body        = '<p>{{greeting}}</p>

<p>It was a pleasure spending the day with such a talented and motivated group on the Professional Scrum Product Owner Advanced course {{course_dates}}. One of the best parts of this course is the chance to learn from each other, share challenges, and discover ways we can all improve together. Thank you for bringing the course to life and contributing to the discussions.</p>

{{delay_note}}

<h3>&#129309; Stay Connected</h3>
<p>Thank you for choosing us as your training partner. We&rsquo;d love to stay in touch:</p>
<ul>
  <li>Connect with me on <a href="https://www.linkedin.com/in/ukalexbrown">LinkedIn</a></li>
  <li>Follow <a href="https://www.linkedin.com/company/bagile">b-agile on LinkedIn</a></li>
</ul>

<h3>&#128205; Your Next Steps</h3>
<p>Your class will be registered with Scrum.org today. Scrum.org is based in Chicago, so you should receive your assessment password by this evening UK time. If you don&rsquo;t see it by tomorrow morning, please let me know.</p>
<p>Once registered, this course will also show on your Scrum.org profile under <strong>My Classes</strong> with the class name and trainer, regardless of whether you sit the assessment.</p>

<h3>&#128203; Assessment Information</h3>
<ul>
  <li>You can get a free retake if you take the assessment within 14 days and don&rsquo;t pass at 85%. The 14 days start once Scrum.org registers you.</li>
  <li>There&rsquo;s no expiry on the assessment key, so take it whenever you&rsquo;re ready.</li>
  <li>PSPO II is more scenario-based than PSPO I, so think practical real-world Product Owner situations rather than memorising the Scrum Guide.</li>
</ul>

<h3>&#127891; Study Tips to Help You Pass</h3>
<p>Preparation makes a huge difference. Here are some specific tips to help you succeed:</p>
<ul>
  <li>Read the <a href="https://www.scrum.org/resources/scrum-guide">Scrum Guide</a> thoroughly to understand core principles.</li>
  <li>Read the <a href="https://www.scrum.org/resources/evidence-based-management-guide">Evidence-Based Management Guide</a> for additional insights on outcome focus.</li>
  <li>Review the <a href="https://www.scrum.org/pathway/product-owner/">Product Owner learning path</a> for key areas to focus on.</li>
  <li>Take the <a href="https://www.scrum.org/open-assessments/product-owner-open">Product Owner Open Assessment</a> multiple times to practice and familiarise yourself with the format. Note that PSPO II questions are more scenario-based.</li>
  <li>Take the <a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open Assessment</a> for additional practice.</li>
  <li>Aim to consistently score 100% on the Product Owner Open Assessment (at least three times in a row).</li>
</ul>

<h3>&#128161; Helpful Resources</h3>
<p>All course materials, including the student reference booklet, are available in your Scrum.org profile. Go to <strong>My Classes</strong> and click <strong>Go to Classroom</strong> next to this course.</p>

<p>Useful B-Agile blogs:</p>
<ul>
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
  <li><a href="https://www.liberatingstructures.com/">Liberating Structures</a></li>
  <li><a href="https://www.scrum.org/resources/evidence-based-management">Evidence-Based Management (EBM)</a></li>
  <li><a href="https://medium.com/serious-scrum/how-to-empower-your-safe-pos-to-become-true-product-owners-af3f29a480ae">How to Empower Your SAFe POs to Become True Product Owners</a></li>
  <li><a href="https://effectiveagile.com/canvases/product-goal-canvas/">Product Goal Canvas</a></li>
  <li><a href="https://www.ted.com/talks/simon_sinek_how_great_leaders_inspire_action">Simon Sinek, How Great Leaders Inspire Action</a> (TED Talk)</li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<p>Here are a few books mentioned in the course:</p>
<ul>
  <li><a href="https://amzn.to/3Nu6khU">Impact Mapping</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/3Tto7cL">User Story Mapping</a> by Jeff Patton</li>
  <li><a href="https://amzn.to/3M4oO80">Lean UX</a> by Jeff Gothelf and Josh Seiden</li>
  <li><a href="https://amzn.to/4903L0i">Lean Enterprise: How High-Performance Organizations Innovate at Scale</a></li>
  <li><a href="https://amzn.to/46TzYEd">Better Value Sooner Safer Happier</a> by Jon Smart</li>
  <li><a href="https://amzn.to/3QoMPYI">Good to Great: Why Some Companies Make the Leap and Others Don&rsquo;t</a> by Jim Collins</li>
  <li><a href="https://amzn.to/3CseqVC">Extreme Ownership: How U.S. Navy SEALs Lead and Win</a></li>
  <li><a href="https://amzn.to/3Co0NH4">Unlocking Business Agility with Evidence-Based Management</a></li>
  <li><a href="https://amzn.to/4epSxnf">The Lean Startup</a> by Eric Ries</li>
</ul>

<h3>&#11088; We&rsquo;d Love Your Review</h3>
<p>Please share your feedback through the <strong>Review the Class</strong> button on your Scrum.org profile. Your insights help us improve and guide others considering the course.</p>

<h3>&#128640; What&rsquo;s Next for You</h3>
<p>Looking to continue your agile journey? Here&rsquo;s how we can support you:</p>
<ul>
  <li><strong>Public Courses:</strong> Browse our <a href="https://www.bagile.co.uk/our-courses/">upcoming courses</a>. Returning customers get 10% off with code <strong>RETURN10</strong>.</li>
  <li><strong>Private Training:</strong> See our <a href="https://www.bagile.co.uk/private-agile-training/">Private Training</a> page for in-house options.</li>
  <li><strong>Coaching &amp; Consulting:</strong> Need agile coaching or consulting? Drop us a line at info@bagile.co.uk.</li>
</ul>

<p>Wishing you the best of luck with your assessments.</p>

<p><strong>Scrum On!</strong><br>
{{trainer_name}}</p>',
    updated_at = NOW()
WHERE course_type = 'PSPO-A';
