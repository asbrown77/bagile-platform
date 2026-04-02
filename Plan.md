# BAgile Platform — Plan

> **Vision:** The golden source for BAgile Ltd's training business — all course, student, enrolment, order, organisation, and payment data in one place, queryable and actionable by Claude via MCP and the BAgile Dashboard.

---

## Product Goals

### Current: Run Courses Confidently
_"I open the dashboard, see my courses, know who's coming, export for Scrum.org, and handle cancellations — all from one place."_

### Next: Know Your Business
_"I can see revenue, partner value, course demand, and make scheduling decisions with data, not gut feel."_

---

## Sprint Queue

*All sprints through 19 complete.*

### Sprint 11: Course Management Hub
**Goal:** The course detail page is a trainer's daily tool — everything needed to run a course in one place.
**We'll know it worked when:** A trainer can click a course, see attendees, export for Scrum.org with country codes, and manage cancellations/transfers from the dashboard.

| # | Item | Size |
|---|------|------|
| 1 | Cancel course button on course detail page | S |
| 2 | Per-attendee refund/transfer actions on course detail | M |
| 3 | Pending transfers view (dashboard-wide) | S |
| 4 | Country code populated from ETL + editable per attendee | M |
| 5 | Scrum.org export button with correct filename | S |

### Sprint 12: Partner & Organisation Analytics
**Goal:** Know which companies book most, which partners are at which tier, and flag when a partner should be upgraded.
**We'll know it worked when:** Dashboard shows top companies by bookings/spend, partner tier status, and alerts for missing PTN coupons.

| # | Item | Size |
|---|------|------|
| 1 | Organisation enrichment: partner_type, discount_rate, PTN tier (V31 migration) | M |
| 2 | Seed known partners from PTN list | S |
| 3 | Company analytics endpoint: bookings, spend, courses by org | M |
| 4 | Fuzzy company name matching | M |
| 5 | Partner dashboard page: tier status, annual bookings, missing coupon alerts | L |
| 6 | MCP tools for org/partner queries | S |

### Sprint 13: Course Demand & Scheduling Analytics
**Goal:** See which courses sell and which don't, so scheduling decisions are data-driven.
**We'll know it worked when:** Dashboard shows bookings per course type over time, fill rates, and booking lead times.

| # | Item | Size |
|---|------|------|
| 1 | Bookings per course type endpoint (PSM, PSPO, etc.) | M |
| 2 | Historical fill rates by course type | M |
| 3 | Booking lead times (how far ahead do people book?) | M |
| 4 | Dashboard analytics page with charts | L |

---

## Bug: ETL Creates Duplicate Enrolments When Attendee Email Changes

**Status:** OPEN — discovered 1 Apr 2026
**Impact:** Course attendee lists show duplicates (e.g. PSPO-300326-AB shows 11 instead of 8)

**Root cause:** `EnrolmentRepository.UpsertAsync` matches by `student_id + order_id + course_schedule_id`. When an attendee email is corrected in WooCommerce, `StudentRepository.UpsertAsync` creates a NEW student (email is unique key). The enrolment upsert then can't find the old enrolment (wrong student_id) and creates a duplicate.

**Affected files:**
- `Bagile.EtlService/Services/WooOrderService.cs:93-111` — ticket processing loop
- `Bagile.Infrastructure/Repositories/EnrolmentRepository.cs:16-59` — upsert logic

**Fix:** Match existing enrolments by `order_id + course_schedule_id` (not student_id). When found, update the student_id on the existing enrolment rather than creating a new one. Need to handle multi-ticket orders (e.g. order 12874 had 2 tickets).

**Cleanup needed:** Enrolments 1796, 15, 19 on course 130 are orphaned duplicates with old emails. Need removing or marking cancelled.

---

## Next Sprint: 21 — "Pre-Course Emails & Quality"

**Goal:** "Send Joining Details actually sends a proper email with agenda and joining info. The platform is tested and reliable."

**We'll know it worked when:** Before the Bristol PSM on 27 Apr, we can click "Send Joining Details" on the course detail page, it loads a PSM template with the venue/dates pre-filled, we preview and edit, test send to ourselves, then send to all 19 attendees. And we have confidence the code won't break because key paths are tested.

| # | Item | Size | Status |
|---|------|------|--------|
| 1 | Pre-course template table (separate from post-course, keyed by course type) | S | READY |
| 2 | Capture and seed PSM pre-course template from NHS Wales email | S | READY |
| 3 | "Send Joining Details" compose flow — load template, pre-fill venue/zoom/dates from course record, editable before sending, test send to trainer | L | READY |
| 4 | Pre-course template editor in settings (separate tab from post-course) | S | READY |
| 5 | Trainer email lookup from trainers table (replace hardcoded lookup in test send) | S | READY |
| 6 | Scrum.org course badge on course detail header | S | READY |
| 7 | Email send audit log — record who sent what to whom, when | M | READY |
| 8 | Portal E2E test: login → dashboard → course detail → send test email | M | READY |
| 9 | Unit tests: email template variable substitution | S | READY |
| 10 | Unit tests: student override + ETL interaction | S | READY |

### 3 Amigos — Pre-Course Compose Flow

**Tester:**
- What if venue/zoom details are empty on the course? → Show warning, still allow send
- What if template doesn't exist for this course type? → Show "no template" message, allow composing from scratch
- What if attendee list is empty? → Disable send button
- Test send should include [TEST] prefix like post-course

**Dev:**
- Reuse the same email service (SmtpEmailService) and send pattern
- Pre-course templates need variables: `{{course_name}}`, `{{dates}}`, `{{times}}`, `{{trainer_name}}`, `{{venue_address}}`, `{{zoom_url}}`, `{{zoom_id}}`, `{{zoom_passcode}}`, `{{self_study}}`, `{{agenda_day1}}`, `{{agenda_day2}}`
- The compose flow needs an HTML editor (contentEditable or textarea with preview toggle — same pattern as post-course)
- Store as `pre_course_templates` table (same structure as post_course_templates)

**PO:**
- Each course type has different agenda and self-study (PSM ≠ PSPO ≠ PSK)
- Virtual vs F2F templates differ (Zoom details vs venue address)
- The trainer MUST be able to edit before sending — this isn't just a template, it's a compose flow
- Bristol PSM on 27 Apr is the first real test — needs to work by then

---

## Pull Backlog

Items below are prioritised but not yet scheduled.

### P2 — Payment Visibility

| ID | Item | Who Needs It | Status |
|----|------|-------------|--------|
| X1 | Xero investigation — map OAuth, spike single invoice | Payment visibility | Ready to refine |
| X2 | Re-enable Xero collector (or new approach) | Payment visibility | Blocked by X1 |
| X3 | Add payment_status to orders | Daily operations | Blocked by X2 |
| S1 | Stripe payment check endpoint (both accounts) | Daily operations | Ready to refine |

### P3 — Automation & Outreach

| ID | Item | Who Needs It | Status |
|----|------|-------------|--------|
| M1 | MailChimp integration — sync past students | Marketing | Deferred |
| SC1 | Scrum.org sync (Playwright) — course listings, assessments | Course admin | Deferred |
| N1 | n8n automation — PTN coupon validation on QA orders | Revenue protection | Ready to refine |

### P4 — Technical Health (from architecture review)

| # | Item | Size | Priority |
|---|------|------|----------|
| 1 | Add FluentValidation for all write commands | 1 day | Medium |
| 2 | ASP.NET rate limiting middleware | 2 hrs | Medium |
| 3 | Request logging with correlation IDs (Serilog) | 0.5 day | Medium |
| 4 | Move portal auth to httpOnly cookie (eliminate localStorage key) | 2-3 days | Low |
| 5 | ETL retry with exponential backoff + circuit breaker | 2 days | Medium |
| 6 | Extract SQL filter builder (eliminate string concatenation) | 0.5 day | Low |
| 7 | Make CORS origins environment-configurable | 30 min | Low |

### Deferred

- **Leads table** — deferred until CRM tool decision (HubSpot Free vs custom). Trello works for now.
- **Communication log** — evaluate as part of CRM decision.
- **Zoom integration** — need to investigate where Zoom details are stored (WooCommerce product meta?)
- **Bulk edit Organisation/Country** for public course attendees — low priority
- **Settings page redesign** — tabbed layout (General, Templates, Trainers, API Keys) — cosmetic

---

## Completed

### Sprint 21 extras (2 Apr 2026)
- [x] Trainers table + API + settings UI + dropdown on course panels
- [x] Send Test to Me feature (test email to trainer before sending to attendees)
- [x] Clearer Send dropdown with descriptions
- [x] 5 critical architecture fixes (usage tracking, Xero async, SMTP throw, pageSize cap, webhook null)
- [x] UX quick wins: split info cards, smart primary button, capacity bar, contextual actions, favicon

### Sprint 20 — "Complete the Toolkit" (Completed 2 Apr 2026)
- [x] Course Contacts section for private courses (admin/organiser)
- [x] Auto-detect organisation from email domain
- [x] Company as 4th column in paste attendee format
- [x] Inline List/Calendar toggle on courses page
- [x] Format indicator (V/F2F) on calendar tiles
- [x] Template preview pane (Source/Preview toggle)
- [x] CI versioning fix (dev vs latest Docker tags)

### Sprint 19 — "Private Course Polish" (Completed 2 Apr 2026)
- [x] Edit private course details (slide-over panel)
- [x] Remove attendee from private course
- [x] Hide Transfer/Refund on private courses
- [x] Simplified attendee table for private (no Org/Country)
- [x] Client organisation parsed from title
- [x] Over-capacity visual warning
- [x] Bug fixes: d-away count, cancelled-as-running (ETL guard), template editor visibility

### Sprint 18 — "Complete the Email Loop" (Completed 2 Apr 2026)
- [x] SMTP config for production
- [x] Seed 12 post-course templates (all course types)
- [x] Template editor UI in portal settings
- [x] MCP tools (update_student, send_post_course_email)
- [x] Monitoring API: expose course status

### Sprint 17 — "Calendar Enhancement & Visual Identity" (Completed 2 Apr 2026)
- [x] Fix cancelled courses showing as "Running" on dashboard
- [x] Course colour system (scrum.org badge colours)
- [x] Calendar tile redesign (left border, trainer circle, enrolments, multi-day)
- [x] Trainer filter (AB/CB/Both)
- [x] Dashboard 5-day week strip with navigation
- [x] Cancelled courses toggle (hidden by default)
- [x] Fix "d away" interpolation bug
- [x] Fix "publish" status badge

---

## Completed

### Sprint 20 — "Complete the Toolkit" (Completed 2 Apr 2026)
- [x] Course Contacts section for private courses (admin/organiser)
- [x] Auto-detect organisation from email domain
- [x] Company as 4th column in paste attendee format
- [x] Inline List/Calendar toggle on courses page
- [x] Format indicator (V/F2F) on calendar tiles
- [x] Template preview pane (Source/Preview toggle)
- [x] CI versioning fix (dev vs latest Docker tags)

### Sprint 19 — "Private Course Polish" (Completed 2 Apr 2026)
- [x] Edit private course details (slide-over panel)
- [x] Remove attendee from private course
- [x] Hide Transfer/Refund on private courses
- [x] Simplified attendee table for private (no Org/Country)
- [x] Client organisation parsed from title
- [x] Over-capacity visual warning
- [x] Bug fixes: d-away count, cancelled-as-running (ETL guard), template editor visibility

### Sprint 18 — "Complete the Email Loop" (Completed 2 Apr 2026)
- [x] SMTP config for production
- [x] Seed 12 post-course templates (all course types)
- [x] Template editor UI in portal settings
- [x] MCP tools (update_student, send_post_course_email)
- [x] Monitoring API: expose course status

### Sprint 17 — "Calendar Enhancement & Visual Identity" (Completed 2 Apr 2026)
- [x] Fix cancelled courses showing as "Running" on dashboard
- [x] Course colour system (scrum.org badge colours)
- [x] Calendar tile redesign (left border, trainer circle, enrolments, multi-day)
- [x] Trainer filter (AB/CB/Both)
- [x] Dashboard 5-day week strip with navigation
- [x] Cancelled courses toggle (hidden by default)
- [x] Fix "d away" interpolation bug
- [x] Fix "publish" status badge

### Sprint 14 (31 Mar 2026)
- [x] Course detail: attendees with orders, country, revenue, cancel/transfer actions
- [x] Dashboard status filter: Running, Completed, Guaranteed, Monitor, At Risk
- [x] Show courses from 2 days ago; use end date for running/completed

### Sprint 12-13 (31 Mar 2026)
- [x] V32: organisations table with aliases, PTN tiers, seeded 19 orgs including all PTN partners
- [x] GET /api/analytics/organisations — top companies by spend with fuzzy matching
- [x] GET /api/analytics/partners — PTN tier status, calculated vs current, upgrade flags
- [x] GET /api/analytics/course-demand — bookings by type, fill rates, monthly trends
- [x] MCP tools: get_organisation_analytics, get_partner_analytics, get_course_demand

### Sprint 11 (31 Mar 2026)
- [x] V31: expanded enrolment status constraint for refund/transfer workflow
- [x] Fixed pending transfers query (cs.updated_at → cs.last_synced)
- [x] All transfer/refund endpoints verified working

### Sprint 10 (31 Mar 2026)
- [x] Revenue summary endpoint + dashboard cards (£ this month, £ this year, monthly breakdown)

### Sprint 9 (31 Mar 2026)
- [x] Cancel + transfer workflow API: mark refund, mark transfer, transfer to course, cancel with actions
- [x] MCP tools for transfers. V30 migration.

### Sprint 8 (31 Mar 2026)
- [x] Scrum.org CSV export (First Name, Last Name, Email, Country). V29 migration. ETL billing_country.

### Sprint 7 (30 Mar 2026)
- [x] Removed automatic transfer heuristic. Fixed PSPO 30-31 Mar: 8 correct attendees.

### Sprint 6 (30 Mar 2026)
- [x] Multi-ticket enrolments, portal + dashboard MVP, API key management, MCP standalone repo.

### Sprint 5 (30 Mar 2026)
- [x] D5: Order lookup by WooCommerce ID. D6: ETL order status sync.

### Sprint 4 (27 Mar 2026)
- [x] D1-D4: Deduplication, data cleanup, dead tests, ETL interval config.

### Sprint 3 (26 Mar 2026)
- [x] APS-SD parser fix, CI fix, GHCR auth, repo separation.

### Sprint 2 (26 Mar 2026)
- [x] Course monitoring endpoint, cancel course endpoint, MCP tools, deployed.

### Sprint 1 (26 Mar 2026)
- [x] ETL double delay fix, MCP server, production API key.
