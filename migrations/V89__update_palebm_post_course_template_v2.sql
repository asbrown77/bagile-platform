-- V89: Update PAL-EBM post-course follow-up template to v2
--
-- Source: Alex Brown's actual sent PAL-EBM follow-up email from 13 Apr 2026
-- (subject "PAL-EBM Training Follow-Up: Next Steps & Resources to Help You
-- Succeed", thread 19d87b4e29f6715b in alexbrown@bagile.co.uk sent folder).
-- Also includes the Velocity blog Alex added in his follow-up message that
-- evening (https://www.bagile.co.uk/using-velocity-forecasting-risk/).
--
-- Two universal additions on top of Alex's content:
--   1. Scrum.org profile My Classes association point
--   2. {{trainer_name}} sign-off
--
-- Style rules: zero em-dashes, Scrum.org-official practice tests only.

UPDATE bagile.post_course_templates
SET subject_template = 'PAL-EBM Training Follow-Up: Next Steps & Resources to Help You Succeed',
    html_body        = '<p>{{greeting}}</p>

<p>It was a pleasure spending the day with such a talented and motivated group on the Professional Agile Leadership Evidence-Based Management course {{course_dates}}. Thank you for bringing the course to life and contributing so much to our discussions.</p>

{{delay_note}}

<h3>&#129309; Stay Connected</h3>
<p>Thank you for choosing us as your training partner. We&rsquo;d love to stay in touch:</p>
<ul>
  <li>Connect with me on <a href="https://www.linkedin.com/in/ukalexbrown">LinkedIn</a></li>
  <li>Follow <a href="https://www.linkedin.com/company/bagile">b-agile on LinkedIn</a></li>
</ul>

<h3>&#128205; Your Next Steps</h3>
<p>Your class will be registered with Scrum.org today, so you should receive your assessment password by this evening. If you don&rsquo;t receive it or have any issues, please let me know.</p>
<p>Once registered, this course will also show on your Scrum.org profile under <strong>My Classes</strong> with the class name and trainer, regardless of whether you sit the assessment.</p>

<h3>&#128203; Assessment Information</h3>
<ul>
  <li>You can get a free retake if you take the assessment within 14 days and don&rsquo;t pass at 85%. The 14 days start once Scrum.org registers you.</li>
  <li>There&rsquo;s no expiry on the assessment key, so take it whenever you&rsquo;re ready.</li>
</ul>

<h3>&#127891; Study Tips to Help You Pass</h3>
<p>Please review the <a href="https://www.scrum.org/resources/evidence-based-management-guide">EBM Guide</a> and these <a href="https://www.scrum.org/resources/suggested-resources-professional-agile-leadershiptm-evidence-based-management">suggested resources</a> before taking the assessment.</p>

<p>Many past students have shared that preparation made a huge difference in passing the assessment. Here are some tips to help you succeed:</p>
<ul>
  <li>Read the <a href="https://www.scrum.org/resources/evidence-based-management-guide">Evidence-Based Management Guide</a>.</li>
  <li>Create a <a href="https://www.scrum.org/">Scrum.org</a> account (required to take the assessment) if you don&rsquo;t have one.</li>
  <li>Take the <a href="https://www.scrum.org/assessments/evidence-based-management-open">EBM Open Assessment</a> for practice.</li>
  <li>Aim for 100% on the open assessment at least three times in a row.</li>
</ul>

<h3>&#128161; Helpful Resources</h3>
<p>All course materials, including the student reference booklet and murals, are available in your Scrum.org profile. Go to <strong>My Classes</strong> and click <strong>Go to Classroom</strong> next to this course.</p>

<p>Useful B-Agile blogs:</p>
<ul>
  <li><a href="https://www.bagile.co.uk/why-agile-fails/">Why Agile Fails: The Mindset Shift Organisations Miss</a></li>
  <li><a href="https://www.bagile.co.uk/escaping-the-product-owners-trap/">Escaping the Product Owner&rsquo;s Trap: A Path to Unleashing True Value</a></li>
  <li><a href="https://www.bagile.co.uk/unmasking-the-product-owner-accountability/">Unmasking the Product Owner: The Accountability Beyond Backlogs</a></li>
  <li><a href="https://www.bagile.co.uk/definition-of-done-where-to-start/">Definition of Done. Where to Start?</a></li>
  <li><a href="https://www.bagile.co.uk/using-velocity-forecasting-risk/">Still Using Velocity? Make It Useful Again</a></li>
</ul>

<p>Impact Mapping:</p>
<ul>
  <li><a href="https://www.impactmapping.org/about.html">Impact Mapping</a> (site)</li>
  <li><a href="https://www.youtube.com/watch?v=yQzIfKzU9PI">Impact Mapping and Story Mapping</a> (video)</li>
  <li><a href="https://www.youtube.com/watch?v=govutBfXPQ">Gojko Adzic, Fast-track from Idea to Impact</a> (video)</li>
</ul>

<p>EBM, OKRs and outcome focus:</p>
<ul>
  <li><a href="https://www.scrum.org/resources/evidence-based-management-guide">Evidence-Based Management (EBM) Guide</a></li>
  <li><a href="https://www.scrum.org/resources/blog/okrs-good-bad-and-ugly">OKRs: The Good, The Bad, and the Ugly</a></li>
  <li><a href="https://www.scrum.org/resources/blog/increase-transparency-outcome-focused-product-roadmaps">Increase Transparency with Outcome-Focused Product Roadmaps</a></li>
  <li><a href="https://www.scrum.org/resources/blog/measure-business-opportunities-unrealized-value">Measure Business Opportunities with Unrealized Value</a></li>
  <li><a href="https://www.youtube.com/watch?v=OqmdLcyES_Q">Turn the Ship Around by L. David Marquet</a> (Google Talk)</li>
  <li><a href="https://www.whatmatters.com/">What Matters (OKRs 101) by John Doerr</a></li>
  <li><a href="https://medium.com/serious-scrum/how-to-empower-your-safe-pos-to-become-true-product-owners-af3f29a480ae">How to Empower Your SAFe POs to Become True Product Owners</a></li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<p>To deepen your understanding, here are a few books I mentioned during the course:</p>
<ul>
  <li><a href="https://amzn.to/46TzYEd">Better Value Sooner Safer Happier</a> by Jon Smart</li>
  <li><a href="https://amzn.to/3Nu6khU">Impact Mapping</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/4akRNig">Specification by Example</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/4davKLk">Measure What Matters</a> by John Doerr</li>
  <li><a href="https://amzn.to/3M4oO80">Lean UX</a> by Jeff Gothelf and Josh Seiden</li>
  <li><a href="https://amzn.to/4naUbNY">Good To Great</a> by Jim Collins</li>
  <li><a href="https://amzn.to/3XLwkK7">Turn the Ship Around!</a> by L. David Marquet</li>
</ul>

<h3>&#11088; We&rsquo;d Love Your Review</h3>
<p>Please share your feedback through the <strong>Review the Class</strong> button on your Scrum.org profile. Your insights help us improve and guide others considering the course. Feel free to mention us in any posts about the class or your certification journey, we&rsquo;d love to celebrate with you.</p>

<h3>&#128640; What&rsquo;s Next for You</h3>
<p>If you&rsquo;re interested in furthering your skills, here&rsquo;s how we can continue to support you:</p>
<ul>
  <li><strong>Public Courses:</strong> Browse our <a href="https://www.bagile.co.uk/our-courses/">upcoming courses</a>. Returning customers get 10% off with code <strong>RETURN10</strong>.</li>
  <li><strong>Private Training:</strong> See our <a href="https://www.bagile.co.uk/private-agile-training/">Private Training</a> page for in-house options.</li>
  <li><strong>Coaching &amp; Consulting:</strong> Need agile coaching or consulting? Drop us a line at info@bagile.co.uk.</li>
</ul>

<p>Wishing you the best of luck with your assessment.</p>

<p><strong>Scrum On!</strong><br>
{{trainer_name}}</p>',
    updated_at = NOW()
WHERE course_type = 'PAL-EBM';
