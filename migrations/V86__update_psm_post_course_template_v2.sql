-- V86: Update PSM post-course follow-up template to v2 enhanced
--
-- Replaces the V39 thin seeded PSM template with the comprehensive version
-- that mirrors what trainers have been sending manually plus added value:
--   • Mikhail Lapshin's free mock exams (community gold standard)
--   • Official Scrum.org PSM Suggested Resources page (corrects wrong PAL-EBM URL in V39)
--   • Nexus Guide and EBM Guide
--   • Developer Open assessment
--   • PSM II 40% assessment discount nudge
--   • Scrum.org profile "My Classes" association explanation
--   • Exam-day strategy tips (timing, marking, most-tested topics)
--   • "What if you don't pass" reassurance
--   • Recommended Reading grouped into 4 themes (foundations / SM skills / product /
--     leadership) with 4 added leadership/team books
--   • Continuous Learning section (forums, conferences, meetups, Scrum Pulse)
--   • What's Next cross-sell to PSM-A, PSM-AI Essentials, PSPO, PSU, APS-SD
--
-- Body intentionally omits <!DOCTYPE>/<html>/<body> tags and the contact footer —
-- EmailTemplateWrapper.Wrap() supplies the branded shell, header and footer.
-- Sign-off uses {{trainer_name}} so the template works for both Alex and Chris.
--
-- Subject also updated to match what trainers have been sending from Gmail,
-- replacing "Your PSM resources and next steps" with the warmer customer-facing
-- "PSM Training Follow-Up: Next Steps & Resources to Help You Succeed".
--
-- Single quotes are doubled per SQL standard. Apostrophes inside content use the
-- &rsquo; HTML entity to avoid SQL escaping noise.

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
<p>Once registered, this course will also show on your Scrum.org profile under <strong>My Classes</strong> with the class name and trainer. That&rsquo;s your record of attendance regardless of how the assessment goes.</p>

<h3>&#128203; Assessment Information</h3>
<ul>
  <li>The PSM I assessment is <strong>80 multiple-choice questions, 60 minutes, 85% to pass</strong>.</li>
  <li>You get <strong>two attempts</strong>: a first attempt now, and a free retake automatically issued if you sit it within 14 days and score under 85%.</li>
  <li>The password has no expiry, so take it when you&rsquo;re ready.</li>
  <li>As a PSM I student, you also qualify for a <strong>40% discount on the PSM II assessment</strong> if you want to push on to the advanced level.</li>
</ul>

<h3>&#127891; How to Prepare</h3>
<p>Most students who put in a few focused hours of prep pass first time. If it doesn&rsquo;t go your way, the free retake gives you a second go without pressure, so don&rsquo;t sweat it.</p>

<p><strong>The official prep stack:</strong></p>
<ul>
  <li>Read the <a href="https://www.scrum.org/resources/scrum-guide">Scrum Guide</a> thoroughly. Make sure you know the accountabilities, artifacts and their commitments, and the events.</li>
  <li>Work through the <a href="https://www.scrum.org/pathway/scrum-master/">Scrum Master learning path</a>, focusing on Understanding and Applying the Scrum Framework, and Developing People and Teams.</li>
  <li>Review Scrum.org&rsquo;s <a href="https://www.scrum.org/resources/suggested-resources-professional-scrum-mastertm">Suggested Resources for PSM</a>.</li>
  <li>Read the <a href="https://www.scrum.org/resources/nexus-guide">Nexus Guide</a> and the <a href="https://www.scrum.org/resources/evidence-based-management-guide">EBM Guide</a> &mdash; both short and useful background.</li>
</ul>

<p><strong>Practice tests:</strong></p>
<ul>
  <li><a href="https://www.scrum.org/open-assessments/scrum-open">Scrum Open Assessment</a> &mdash; aim to score 100% three times in a row before sitting PSM I.</li>
  <li><a href="https://www.scrum.org/open-assessments/product-owner-open">Product Owner Open Assessment</a> &mdash; broader Scrum perspective.</li>
  <li><a href="https://www.scrum.org/open-assessments/developer-open">Developer Open Assessment</a> &mdash; useful for the team-side questions.</li>
  <li><a href="https://mlapshin.com/index.php/scrum-quizzes/">Mikhail Lapshin&rsquo;s free PSM I mocks</a> &mdash; community gold standard, harder than the Scrum Open and great for stretching yourself.</li>
</ul>

<p><strong>Exam-day tips:</strong></p>
<ul>
  <li>Find a quiet hour where you won&rsquo;t be interrupted.</li>
  <li>Have the <a href="https://www.scrum.org/resources/scrum-guide">Scrum Guide</a> open in another tab &mdash; you can reference it during the assessment.</li>
  <li>Manage your time: 60 minutes for 80 questions is roughly 45 seconds each. If you&rsquo;re stuck on one, mark it and move on rather than burning your time.</li>
  <li>Read each question carefully. Eliminate obviously wrong answers first.</li>
  <li>Mark questions you&rsquo;re unsure of and revisit at the end. Don&rsquo;t second-guess yourself on the first pass.</li>
  <li>Trust the Scrum Guide over your team&rsquo;s local practice when the two conflict.</li>
  <li>Most-tested topics are accountabilities (Scrum Master, Product Owner, Developers), the events, and the artifacts with their commitments. Know these cold.</li>
</ul>

<h3>&#128161; Resources for Day-to-Day Scrum Mastery</h3>
<p>All course materials, including the student reference booklet, are available in your Scrum.org profile. Go to <strong>My Classes</strong> and click <strong>Go to Classroom</strong> next to this course.</p>

<p><strong>B-Agile blogs:</strong></p>
<ul>
  <li><a href="https://www.bagile.co.uk/tips-to-pass-the-psm-i-assessment/">10 Powerful Tips to Pass the PSM I Assessment</a></li>
  <li><a href="https://www.bagile.co.uk/what-is-scrum-uncovering-its-true-essence/">What is Scrum? Uncovering Its True Essence</a></li>
  <li><a href="https://www.bagile.co.uk/value-stream-mapping-your-answer/">Where to Start with Scrum? Is Value Stream Mapping Your Answer?</a></li>
  <li><a href="https://www.bagile.co.uk/definition-of-done-where-to-start/">Definition of Done &mdash; Where to Start?</a></li>
  <li><a href="https://www.bagile.co.uk/escaping-the-product-owners-trap/">Escaping the Product Owner&rsquo;s Trap</a></li>
  <li><a href="https://www.bagile.co.uk/agile-estimation-mindset/">Agile Estimation Isn&rsquo;t Broken. But the Thinking Might Be.</a></li>
</ul>

<p><strong>Daily SM toolkit:</strong></p>
<ul>
  <li><a href="https://www.bagile.co.uk/techdebt-simulator/">Tech Debt Simulator</a></li>
  <li><a href="http://retromat.org/">Retromat</a> &mdash; retrospective formats</li>
  <li><a href="https://www.pointingpoker.com/">Planning Poker</a></li>
  <li><a href="https://www.tastycupcakes.org/">TastyCupcakes</a> &mdash; agile games and exercises</li>
  <li><a href="http://www.liberatingstructures.com/">Liberating Structures</a> &mdash; facilitation patterns</li>
</ul>

<p><strong>Mapping techniques:</strong></p>
<ul>
  <li><a href="https://www.youtube.com/watch?v=mqBGO0wgNQM">The Game Has Changed &mdash; Jeff Patton</a></li>
  <li><a href="https://www.jpattonassociates.com/user-story-mapping/">User Story Mapping &mdash; Jeff Patton</a></li>
  <li><a href="https://www.youtube.com/watch?v=yQzIfKzU9PI">Impact Mapping and Story Mapping</a></li>
  <li><a href="https://www.youtube.com/watch?v=govutBfXPQ">Gojko Adzic &mdash; Fast-track from Idea to Impact</a></li>
</ul>

<p><strong>Pair and Mob Programming:</strong></p>
<ul>
  <li><a href="https://collaboration.csc.ncsu.edu/laurie/Papers/XPSardinia.PDF">The Costs and Benefits of Pair Programming (white paper)</a></li>
  <li><a href="https://www.youtube.com/watch?v=SHOVVnRB4h0">Mob Programming: A Whole Team Approach</a></li>
</ul>

<p><strong>Wider Scrum Master practice:</strong></p>
<ul>
  <li><a href="https://www.scrum.org/resources/8-stances-scrum-master">8 Stances of a Scrum Master</a></li>
  <li><a href="https://www.scrum.org/resources/evidence-based-management">Evidence-Based Management (EBM)</a></li>
  <li><a href="https://effectiveagile.com/canvases/product-goal-canvas/">Product Goal Canvas</a></li>
  <li><a href="https://www.scrum.org/resources/blog/day-life-scrum-master">A Day in the Life of a Scrum Master</a></li>
</ul>

<p><strong>Wider Scrum and agile thinking:</strong></p>
<ul>
  <li><a href="https://agilemanifesto.org/">The Agile Manifesto</a> and <a href="https://agilemanifesto.org/principles.html">the 12 Principles</a> &mdash; the foundational text. Worth re-reading every six months.</li>
  <li><a href="https://www.mountaingoatsoftware.com/blog">Mountain Goat Software (Mike Cohn) blog</a> &mdash; long-running agile blog with practical advice.</li>
  <li><a href="https://www.romanpichler.com/blog/">Roman Pichler&rsquo;s blog</a> &mdash; strong on Product Owner and product strategy thinking, useful for SMs supporting POs.</li>
</ul>

<h3>&#128218; Recommended Reading</h3>
<p>A few books I mentioned during the course, grouped by where they help:</p>

<p><strong>Scrum and agile foundations:</strong></p>
<ul>
  <li><a href="https://amzn.to/3sGNhK5">Scrum &mdash; A Pocket Guide</a> by Gunther Verheyen</li>
  <li><a href="https://amzn.to/3sNGazt">Zombie Scrum Survival Guide</a> by Barry Overeem and Christiaan Verwijs (see also <a href="https://www.zombiescrum.org/">zombiescrum.org</a>)</li>
  <li><a href="https://amzn.to/3uAdpXm">Scrum Mastery</a> by Geoff Watts</li>
  <li><a href="https://amzn.to/3SsnbF8">Better Value Sooner Safer Happier</a> by Jon Smart</li>
</ul>

<p><strong>Practical Scrum Master skills:</strong></p>
<ul>
  <li><a href="https://amzn.to/46pdMBw">Agile Retrospectives</a> by Esther Derby</li>
  <li><a href="https://amzn.to/3GaHyPG">Coaching Agile Teams</a> by Lyssa Adkins</li>
  <li><a href="https://amzn.to/3MSFEXJ">Extraordinary Badass Agile Coach</a> by Robert L. Galen</li>
  <li><a href="https://amzn.to/3MVXw3X">Agile Estimating and Planning</a> by Mike Cohn</li>
</ul>

<p><strong>Product, stories and user value:</strong></p>
<ul>
  <li><a href="https://amzn.to/3Nu6khU">Impact Mapping</a> by Gojko Adzic</li>
  <li><a href="https://amzn.to/3Tto7cL">User Story Mapping</a> by Jeff Patton</li>
  <li><a href="https://amzn.to/3uXI2qf">Fifty Quick Ideas To Improve Your User Stories</a> by Gojko Adzic</li>
</ul>

<p><strong>Leadership, teams and culture:</strong></p>
<ul>
  <li><a href="https://amzn.to/3QoMPYI">Good to Great</a> by Jim Collins</li>
  <li><em>Drive</em> by Daniel Pink &mdash; why people are motivated, and why most workplaces get it wrong.</li>
  <li><em>Turn the Ship Around</em> by L. David Marquet &mdash; leader-leader, intent-based leadership in action.</li>
  <li><em>The Five Dysfunctions of a Team</em> by Patrick Lencioni &mdash; short, readable, useful for any Scrum Master coaching team dynamics.</li>
  <li><em>The Phoenix Project</em> by Gene Kim &mdash; DevOps fiction, but the cultural and flow lessons translate directly to Scrum teams.</li>
</ul>

<h3>&#127757; Continuous Learning</h3>
<p>Becoming a great Scrum Master is a journey, not a destination. A few places worth plugging into:</p>
<ul>
  <li><a href="https://www.scrum.org/forum">Scrum.org community forums</a> &mdash; questions, debate, and PST input.</li>
  <li>Scrum Day, Agile XYZ, and regional agile conferences &mdash; many run virtually.</li>
  <li>Local meetups &mdash; search Meetup.com for &ldquo;scrum&rdquo; or &ldquo;agile&rdquo; in your area.</li>
  <li>Scrum.org&rsquo;s <a href="https://www.scrum.org/resources/scrum-pulse">Scrum Pulse webcasts</a> &mdash; short talks from PSTs around the world.</li>
</ul>

<h3>&#11088; We&rsquo;d Love Your Review</h3>
<p>If the course was useful, please leave a review using the <strong>Review the Class</strong> button on your Scrum.org profile. It takes less than five minutes and is the single biggest help in letting future students decide whether we&rsquo;re the right trainers for them.</p>

<h3>&#128640; What&rsquo;s Next for You</h3>
<p>If you&rsquo;d like to keep building on this, a few directions worth considering:</p>

<p><strong>Deeper Scrum Master practice:</strong></p>
<ul>
  <li><strong>PSM-A (advanced):</strong> Our PSM-A course leads to PSM II. Combined with your 40% PSM II assessment discount as a PSM I student, this is the strongest next step for sharpening real-world Scrum Master skills.</li>
  <li><strong>PSM-AI Essentials:</strong> A one-day course on using AI as a Scrum Master. Pairs naturally with what you&rsquo;ve just learned and adds a fast-moving capability to your toolkit.</li>
</ul>

<p><strong>Broaden your perspective:</strong></p>
<ul>
  <li><strong>PSPO (Professional Scrum Product Owner):</strong> Understand the Product Owner side. Even if you stay in the Scrum Master accountability, knowing what good looks like for the PO makes you a far better partner to the team.</li>
  <li><strong>PSU (Professional Scrum with User Experience):</strong> Bringing UX practices into Scrum. Useful if your team is product-focused.</li>
  <li><strong>APS-SD (Applying Professional Scrum for Software Development):</strong> Three-day, hands-on course where teams build software together over Sprints with modern engineering practices. Great for software teams looking to deepen Done.</li>
</ul>

<p><strong>Browse and book:</strong></p>
<ul>
  <li><strong>Public Courses:</strong> <a href="https://www.bagile.co.uk/our-courses/">All our upcoming courses</a>. Returning customers get 10% off with code <strong>RETURN10</strong>, and groups of 3+ from the same organisation get an automatic 10% off.</li>
  <li><strong>Private Training:</strong> For in-house training tailored to your team, see our <a href="https://www.bagile.co.uk/private-training/">Private Training</a> page.</li>
  <li><strong>Coaching &amp; Consulting:</strong> For agile coaching or consulting support, drop us a line at info@bagile.co.uk.</li>
</ul>

<p><strong>Stay in touch:</strong></p>
<ul>
  <li><strong>B-Agile blog:</strong> New posts on <a href="https://www.bagile.co.uk/">bagile.co.uk</a> &mdash; practitioner-focused, anti-hype.</li>
  <li><strong>LinkedIn:</strong> Follow <a href="https://www.linkedin.com/company/bagile">b-agile</a> for upcoming course announcements and community content.</li>
</ul>

<p>Wishing you success in your assessment and in everything beyond it.</p>

<p>If you have any questions, feel free to reach out.</p>

<p><strong>Scrum On!</strong><br>
{{trainer_name}}</p>',
    updated_at = NOW()
WHERE course_type = 'PSM';
