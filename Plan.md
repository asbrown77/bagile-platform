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

*All sprints through 14 complete.*

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

## Next Sprint: 15 — "Portal Polish, Calendar View & Course Risk Config"

**Goal:** The portal looks professional, has a calendar view for quick schedule overview, and course risk thresholds are configurable.
**We'll know it worked when:** Browser tab shows BAgile favicon + correct title. Calendar view shows this week/month at a glance with status, trainer, enrolments. Courses at 0 days show "cancel" not "at risk".

| # | Item | Size | Status |
|---|------|------|--------|
| 1 | Favicon — copy BAgile logo to portal, add to layout | XS | READY |
| 2 | Page title — confirm/fix "b-agile portal" in browser tab | XS | READY |
| 3 | Course at-risk configurable threshold — 0 days = cancel, ≤ N days = at risk (N = config, default 2) | M | READY |
| 4 | Course calendar view — week/month toggle with schedule overview | L | READY |
| 5 | Bug: private course title not saved (returns empty from API) | S | READY |

### 4 — Course Calendar View

**What it is:** A calendar-style view of the course schedule accessible from the courses page. Toggle between "this week" and "this month". Shows all courses (public + private) at a glance.

**Each course tile shows:**
- Course name + code
- Date(s)
- Trainer (Alex / Chris)
- Enrolment count / minimum (e.g. "3/3" or "0/4")
- Status badge (Healthy / At Risk / Cancelled / Confirmed)
- Public / Private tag
- Click → course detail page

**Filters:** Trainer, status, public/private, date range
**Colour coding:** Green = healthy/confirmed, Orange = at risk/monitor, Red = cancelled/critical, Blue = private

**Why:** Currently the courses list is a flat table sorted by date. A calendar view gives instant visual overview of what's running, what's at risk, and where the gaps are. Especially useful during morning checks and scheduling decisions.

**Files:**
- `bagile-portal/app/(authenticated)/courses/calendar/page.tsx` (may already exist — enhance)
- Reuse existing course monitoring data from API

**AC:**
- [ ] Week view shows 7 days with courses positioned by date
- [ ] Month view shows full month grid
- [ ] Private courses visible alongside public
- [ ] Status badges match monitoring endpoint
- [ ] Click any course → course detail page
- [ ] Toggle between week/month persists

### 5 — Bug: Private course title empty (API only)

**What it is:** When creating a private course via `POST /api/course-schedules/private`, the title field returns empty even when provided in the request body. Creating via portal UI works fine — title is saved correctly. API-only issue.

**Files:**
- `Bagile.Application/CourseSchedules/Commands/CreatePrivateCourse/` — check if title is mapped from request to entity

### 6 — Bug: Price label shows "per person" for private courses

**What it is:** The course detail page shows price as "£16,200 per person" but for private courses this is the total price, not per-person. The label should say "total" for private courses and "per person" for public courses.

**Files:**
- `bagile-portal/app/(authenticated)/courses/[id]/page.tsx` — conditional label based on course type

**AC:**
- [ ] Private courses show "£X total"
- [ ] Public courses show "£X per person"

### 1 — Favicon

**What it is:** Copy the existing BAgile favicon from the WordPress site to `bagile-portal/public/` and wire it up in the Next.js layout so the browser tab shows the BAgile logo.

**Files:**
- `bagile-portal/public/favicon.png` (copy from WordPress uploads)
- `bagile-portal/app/layout.tsx` — add `<link rel="icon">` or Next.js `icon` metadata

**AC:**
- [ ] Browser tab shows BAgile 3-circles logo
- [ ] Works in Chrome, Firefox, Safari

### 2 — Page title

**What it is:** The metadata title in `layout.tsx` currently says "BAgile Portal". Confirm this is correct or change to user's preferred casing.

**Files:**
- `bagile-portal/app/layout.tsx` — `metadata.title`

**AC:**
- [ ] Browser tab shows correct product name

### 3 — Course at-risk configurable threshold

**What it is:** Replace the hardcoded 7-day / 3-enrolment "at risk" logic with configurable thresholds. Add a new status "cancel" for courses at 0 days until start with low enrolment. The at-risk day threshold (default: 2 days) should be configurable from the dashboard settings page.

**Current hardcoded values (4 locations):**
- `Bagile.Infrastructure/Persistence/Queries/CourseScheduleQueries.cs:45` — `INTERVAL '7 days'` + `< 3`
- `Bagile.Infrastructure/Persistence/Queries/CourseScheduleQueries.cs:141` — same
- `bagile-portal/app/(authenticated)/dashboard/page.tsx:72` — `daysUntilStart <= 7` + `< 3`
- `bagile-portal/app/(authenticated)/layout.tsx:26` — `daysUntilStart <= 7` + `< 3`

**New logic:**
- `daysUntilStart === 0` AND low enrolment → "cancel" (should cancel, too late)
- `daysUntilStart <= AT_RISK_DAYS` AND low enrolment → "at risk" (configurable, default 2)
- `daysUntilStart > AT_RISK_DAYS` → normal ("monitor" / "guaranteed")

**Files:**
- `Bagile.Infrastructure/Persistence/Queries/CourseScheduleQueries.cs` — parameterise the interval
- `bagile-portal/app/(authenticated)/dashboard/page.tsx` — read threshold from config
- `bagile-portal/app/(authenticated)/layout.tsx` — same
- `bagile-portal/app/(authenticated)/courses/page.tsx` — add "cancel" status handling
- `bagile-portal/app/(authenticated)/courses/calendar/page.tsx` — same
- `bagile-portal/components/ui/Badge.tsx` — add "cancel" badge variant
- `bagile-portal/app/(authenticated)/settings/page.tsx` or new config store — threshold setting

**3 Amigos:**
- **Tester:** What if threshold is set to 0? → Everything is either "cancel" or normal, no "at risk" window
- **Dev:** Where to store config? → localStorage for MVP, API-backed setting later
- **Dev:** Backend SQL still uses 7 days — decouple? → Frontend overrides with its own threshold for now; backend SQL broadened to return enough data
- **PO:** Should "cancel" be an action or just a label? → Just a label/badge for now, actual cancel action already exists on course detail

## Pull Backlog

Items below are prioritised but not yet scheduled. Refine before pulling into a sprint.

### P1 — Editable Attendee Details (Override PTN/Partner Data)

**Problem:** PTN partners (NobleProg, QA Ltd, etc.) place orders with their own staff email addresses instead of the actual attendee's email. The ETL syncs these wrong emails into the platform. Currently the only fix is to update WooCommerce order meta AND FooEvents tickets manually, then wait for ETL re-sync. There's no way to correct attendee data directly in the platform.

**Solution:** Allow attendee details (email, first name, last name, company) to be overridden in the platform, with overrides surviving ETL re-syncs.

| # | Item | Size |
|---|------|------|
| 1 | `PUT /api/students/{id}` endpoint — update email, name, company | S |
| 2 | Override flag on student fields — ETL skips fields marked as manually overridden | M |
| 3 | Portal UI: inline-editable attendee fields on course detail page | M |
| 4 | MCP tool: `update_student` for Claude agent access | S |

**Why override flag matters:** The ETL upserts students by email (email is the unique key). Without an override mechanism, the next ETL cycle would either overwrite the correction or create a duplicate student record with the old email.

**Important context for refinement:**

PTN partners (NobleProg, QA Ltd, etc.) often deliberately register with their own email addresses. This isn't always a mistake — they may want to:
- Control ticket delivery and forward to delegates themselves with their own context
- Manage the relationship with their end client (e.g. government departments like Ofgem, DVSA)
- Bundle joining instructions with their own onboarding materials

So this feature needs two distinct concepts:
1. **Platform record override** — correct who actually attended (needed for Scrum.org class submissions, certification records, repeat customer tracking, and analytics). This should NOT trigger any FooEvents ticket resends.
2. **FooEvents/WooCommerce stays as-is** — the partner's original email remains on the ticket/order. Ticket delivery stays under partner control.

Today's real example: NobleProg order #12874/#12884 had 3 attendees registered with `claire.alcock@nobleprog.com` emails. The real attendees were at Ofgem/GPA. We needed the correct emails for Scrum.org submission but NobleProg controls the ticket delivery flow.

The current workaround requires updating 3 systems (FooEvents tickets, WooCommerce order meta, waiting for ETL sync) which is error-prone and slow.

**AC:**
- [ ] Can update attendee email/name/company from course detail page
- [ ] Changes persist through ETL sync cycles (override flag)
- [ ] Updates are platform-only — no side effects on FooEvents tickets or WooCommerce orders
- [ ] MCP tool available so Claude agent can update directly
- [ ] Audit trail: who changed what, when

### P1.5 — Post-Course Follow-Up Emails

**Problem:** After each course, Alex manually sends a follow-up email with assessment info, resources, and book recommendations. Each course type (PSM, PSPO, PSPO-A, PSU, PAL-EBM, etc.) has a unique template with different learning paths, blog links, practice assessments, and reading lists. Currently these live only in Alex's Gmail sent folder and have to be copy-pasted and adapted manually each time.

**Goal:** One-click "Send Follow-Up" from the portal course detail page, with templates also accessible via MCP so the Claude agent can draft them.

| # | Item | Size |
|---|------|------|
| 1 | Template storage — HTML templates per course type with placeholder variables (attendee names, course date, trainer name, delay apology toggle) | M |
| 2 | Template management UI — edit templates from portal settings page (rich text editor) | M |
| 3 | "Send Follow-Up" button on course detail page — select template, preview with real attendee data, confirm and send | L |
| 4 | MCP tool: `generate_post_course_email` — returns populated HTML for a given course schedule ID | S |
| 5 | Seed existing templates — PSM, PSPO, PSPO-A, PSU, PAL-EBM (from email history). Create drafts for missing: PSM-AI, PSPO-AI, PSK, PAL-E, PSM-A, PSFS, APS-SD | M |

**Design considerations:**
- Templates could live in the database (simple, queryable, editable via portal) OR as markdown/HTML files in the repo (version controlled, reviewable). Database is probably better since non-technical users (Alex) need to edit them and they change per course, not per deployment.
- Each template has a `course_type` key (e.g. "PSM", "PSPO") and an HTML body with variables like `{{greeting}}`, `{{delay_note}}`, `{{trainer_name}}`, `{{sign_off}}`
- Portal renders a preview before sending — trainer can review and tweak before it goes out
- Email is sent from alexbrown@bagile.co.uk (or the trainer's email), not info@
- MCP tool returns the populated template so Claude can create a Gmail draft — useful when Alex asks "send the follow-up for last week's PSPO"
- Future: could auto-trigger when scrum.org class is processed (n8n webhook?)

**Templates found (5 of 12+):** PSM, PSPO, PSPO-A, PSU, PAL-EBM — all materially different (different resources, books, learning paths, assessment details). Missing: PSM-AI, PSPO-AI, PSK, PAL-E, PSM-A, PSFS, APS-SD.

### P2 — Payment Visibility

| ID | Item | Who Needs It | Status |
|----|------|-------------|--------|
| X1 | Xero investigation — map OAuth, spike single invoice | Payment visibility | Ready to refine |
| X2 | Re-enable Xero collector (or new approach) | Payment visibility | Blocked by X1 |
| X3 | Add payment_status to orders | Daily operations | Blocked by X2 |
| S1 | Stripe payment check endpoint (both accounts) | Daily operations | Ready to refine |

### P2.5 — Course Contacts & Private Course Enhancements

| # | Item | Size |
|---|------|------|
| 1 | Course Contacts section — store admin/logistics contacts separately from attendees (e.g. Debbie Gooch for invoicing, Stuart Pullin as organiser) | M |
| 2 | Auto-detect organisation from email domain (e.g. @fnc.co.uk → Frazer-Nash Consultancy) | S |
| 3 | Support Company as 4th column in paste attendee format | S |
| 4 | Bulk edit Organisation/Country for attendees after adding | M |
| 5 | Calendar: inline toggle between list and calendar on courses page (not separate page) | M |
| 6 | Calendar: show course type name and format (Virtual/In-person) on tiles | S |

### P3 — Automation & Outreach

| ID | Item | Who Needs It | Status |
|----|------|-------------|--------|
| M1 | MailChimp integration — sync past students | Marketing | Deferred |
| SC1 | Scrum.org sync (Playwright) — course listings, assessments | Course admin | Deferred |
| N1 | n8n automation — PTN coupon validation on QA orders | Revenue protection | Ready to refine |

### Deferred

- **Leads table** — deferred until CRM tool decision (HubSpot Free vs custom). Trello works for now.
- **Communication log** — evaluate as part of CRM decision.
- **Zoom integration** — need to investigate where Zoom details are stored (WooCommerce product meta?)

---

## Completed

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
