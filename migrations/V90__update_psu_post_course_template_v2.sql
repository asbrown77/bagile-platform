-- V90: Update PSU post-course follow-up template to v2
--
-- Source: Alex Brown's actual sent PSU follow-up email from 11 Oct 2024
-- (subject "PSU Training Follow-Up: Next Steps & Resources to Help You
-- Succeed", thread 1927d5949a8c01ec in alexbrown@bagile.co.uk sent folder).
--
-- Two universal additions on top of Alex's content:
--   1. Scrum.org profile My Classes association point
--   2. {{trainer_name}} sign-off
--
-- Style rules: zero em-dashes, Scrum.org-official practice tests only.

UPDATE bagile.post_course_templates
SET subject_template = 'PSU Training Follow-Up: Next Steps & Resources to Help You Succeed',
    html_body        = '<p>{{greeting}}</p>

<p>It was a pleasure spending the last two days with such a talented and motivated group on the Professional Scrum with User Experience course {{course_dates}}. Thank you all for your enthusiasm, great discussions, and for bringing the course to life.</p>

{{delay_note}}

<h3>&#129309; Stay Connected</h3>
<p>Thank you for choosing us as your training partner. We&rsquo;d love to stay in touch:</p>
<ul>
  <li>Connect with me on <a href="https://www.linkedin.com/in/ukalexbrown">LinkedIn</a></li>
  <li>Follow <a href="https://www.linkedin.com/company/bagile">b-agile on LinkedIn</a></li>
</ul>

<h3>&#128205; Your Next Steps</h3>
<p>If you already have a Scrum.org profile and use a different email, please send it to me. You can change your profile email at any time.</p>
<p>Your class will be registered with Scrum.org today, so you should receive your PSU assessment password by this evening UK time. If you don&rsquo;t see it by tomorrow morning, please let me know and I&rsquo;ll follow up for you.</p>
<p>Once registered, this course will also show on your Scrum.org profile under <strong>My Classes</strong> with the class name and trainer, regardless of whether you sit the assessment.</p>

<h3>&#128203; Assessment Information</h3>
<ul>
  <li>You can get a free retake if you take the assessment within 14 days and don&rsquo;t pass at 85%. The 14 days start once Scrum.org registers you.</li>
  <li>There&rsquo;s no expiry on the assessment key, so take it whenever you&rsquo;re ready, but early preparation is often key.</li>
</ul>

<h3>&#127891; Study Tips to Help You Pass</h3>
<p>Many past students have shared that preparation made a huge difference in passing the assessment. Here are some tips to help you succeed:</p>
<ul>
  <li>Read the <a href="https://www.scrum.org/resources/scrum-guide">Scrum Guide</a> thoroughly.</li>
  <li>Review the <a href="https://www.scrum.org/pathway/product-owner/">Product Owner learning path</a> for additional insights.</li>
  <li>Aim for 100% on the <a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open Assessment</a> at least three times before taking the real one.</li>
  <li>Brush up on the Scrum framework and UX concepts covered during the course. Be sure to review the course slides.</li>
</ul>

<h3>&#128161; Helpful Resources</h3>
<p>All course materials, including the student reference booklet and murals, are available in your Scrum.org profile. Go to <strong>My Classes</strong> and click <strong>Go to Classroom</strong> next to this course.</p>

<p>Useful B-Agile blogs:</p>
<ul>
  <li><a href="https://www.bagile.co.uk/why-agile-fails/">Why Agile Fails: The Mindset Shift Organisations Miss</a></li>
  <li><a href="https://www.bagile.co.uk/what-is-scrum-uncovering-its-true-essence/">What is Scrum? Uncovering Its True Essence</a></li>
  <li><a href="https://www.bagile.co.uk/escaping-the-product-owners-trap/">Escaping the Product Owner&rsquo;s Trap: A Path to Unleashing True Value</a></li>
  <li><a href="https://www.bagile.co.uk/definition-of-done-where-to-start/">Definition of Done. Where to Start?</a></li>
</ul>

<p>Impact Mapping:</p>
<ul>
  <li><a href="https://www.impactmapping.org/about.html">Impact Mapping</a> (site)</li>
  <li><a href="https://www.youtube.com/watch?v=yQzIfKzU9PI">Impact Mapping and Story Mapping</a> (video)</li>
  <li><a href="https://www.youtube.com/watch?v=govutBfXPQ">Gojko Adzic, Fast-track from Idea to Impact</a> (video)</li>
</ul>

<p>UX, discovery and experiments:</p>
<ul>
  <li><a href="https://www.scrum.org/resources/evidence-based-management">Evidence-Based Management (EBM)</a></li>
  <li><a href="https://giffconstable.com/2021/04/the-truth-curve-and-the-build-curve/">The Truth Curve and the Build Curve</a> by Giff Constable</li>
  <li><a href="https://uxdesign.cc/heres-what-to-do-when-user-research-doesn-t-fit-in-a-sprint-2f8b5db7d48c">Here&rsquo;s what to do when user research doesn&rsquo;t fit in a sprint</a></li>
  <li><a href="https://www.mindtheproduct.com/product-metric-matters-josh-elman/">The only product metric that matters</a> (video)</li>
  <li><a href="http://theleanstartup.com/principles">Lean Startup Principles</a></li>
  <li><a href="http://bit.ly/PsuNordstromVideo">Nordstrom Innovation Lab</a> (video)</li>
  <li><a href="https://web.archive.org/web/20180802023922/joshuaseiden.com/blog/2013/04/nordstrom-what-happened-next">Nordstrom Innovation Lab: What Happened Next</a></li>
  <li><a href="https://www.youtube.com/watch?v=HdqX4A_3-bA">Hire More Designers, OK?</a> (video)</li>
</ul>

<p>Experiment examples:</p>
<ul>
  <li><a href="https://www.kickstarter.com/projects/ridebeeline/beeline-moto-smart-navigation-for-motorcycles-made">Landing Page (Beeline on Kickstarter)</a></li>
  <li><a href="https://www.youtube.com/watch?v=isKvctKuWFk">Paper Prototype</a> (video)</li>
  <li><a href="https://www.youtube.com/watch?v=F-7cjdtrQ9Y">Service Prototype: The Founder</a> (video)</li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<p>Here are a few books I mentioned during the course:</p>
<ul>
  <li><a href="https://amzn.to/4h6gS43">Impact Mapping</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/402oyOz">User Story Mapping</a> by Jeff Patton</li>
  <li><a href="https://amzn.to/3Nprqxi">Lean UX</a> by Jeff Gothelf and Josh Seiden</li>
  <li><a href="https://amzn.to/3YjkLew">Lean Enterprise</a> by Jez Humble and Barry O&rsquo;Reilly</li>
  <li><a href="https://amzn.to/3BQmZJj">Better Value Sooner Safer Happier</a> by Jon Smart</li>
</ul>

<h3>&#11088; We&rsquo;d Love Your Review</h3>
<p>Please share your feedback through the <strong>Review the Class</strong> button on your Scrum.org profile. Your insights help us improve and guide others considering the course. Feel free to mention us in any posts about the class or your certification journey, we&rsquo;d love to celebrate with you.</p>

<h3>&#128640; What&rsquo;s Next for You</h3>
<p>If you&rsquo;re looking to upskill or deepen your agile expertise, here&rsquo;s how we can continue to support you:</p>
<ul>
  <li><strong>Public Courses:</strong> Browse our <a href="https://www.bagile.co.uk/our-courses/">upcoming courses</a>. Returning customers get 10% off with code <strong>RETURN10</strong>.</li>
  <li><strong>Private Training:</strong> See our <a href="https://www.bagile.co.uk/private-agile-training/">Private Training</a> page for in-house options.</li>
  <li><strong>Coaching &amp; Consulting:</strong> Need agile coaching or consulting? Drop us a line at info@bagile.co.uk.</li>
</ul>

<p>Wishing you the best of luck with your assessment.</p>

<p><strong>Scrum On!</strong><br>
{{trainer_name}}</p>',
    updated_at = NOW()
WHERE course_type = 'PSU';
