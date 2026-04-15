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

*All sprints through 29 complete. Sprint 30 is Bug Fix, Sprint 31 is UX Polish.*

---

### Sprint 30 — "UX Polish" _(next up)_

UX audit completed 15 Apr 2026. Full report: `UX_AUDIT.html` in repo root.

**Quick wins (1 session):**

| # | Item | Size | Status |
|---|------|------|--------|
| U1 | Gateway checklist tick → clickable link to published page | XS | **DONE** |
| U2 | Dashboard: reorder — week strip + at-risk before KPI cards | S | READY |
| U3 | Sidebar: move at-risk badge from /courses to /courseschedule | XS | READY |
| U4 | Dashboard: fix "View all" links → /courseschedule?view=list | XS | READY |
| U5 | Remove "draft" from Badge.tsx statusBadge map | XS | READY |
| U6 | Separate "Decision due" legend from filter pills | XS | READY |
| U7 | Side panel: move Edit button to SlideOver header (add actions prop) | S | READY |
| U8 | Promote "Publish →" to a proper Button for unpublished gateways | S | READY |
| U9 | Empty week-strip cells → link to /courseschedule (add + affordance on hover) | S | READY |
| U10 | PageHeader title: text-xl → text-2xl | XS | READY |

**Structural (sprint-sized, plan separately):**
- Dashboard: operator-first layout — KPI cards to Revenue page, dashboard = cockpit
- Sidebar: merge Course Schedule + Private Courses into cleaner nav structure
- Side panel: state-driven layout (Planned → big Publish CTA; Live → enrolment hero)
- Calendar: "Next up" context strip above calendar
- Payment visibility: when Xero re-integrates, payment status in attendee table + side panel

---

### Sprint 27 — "Course Calendar v2 + Gateway Config" _(deferred)_
_After v1 is live and used for a full month._

- Course types settings page — list of all course types, configure applicable gateways per type
- Internal BAgile course type support
- IC Agile gateway publish automation
- Drag-to-reschedule for planned courses
- Monthly revenue total in calendar footer
- Mobile responsive calendar

---

### Sprint 30 — "Bug Fix Sprint" _(next up)_

**Goal:** Clear the open bug backlog before the next feature sprint.

| # | Bug | Impact | Status |
|---|-----|--------|--------|
| B1 | ETL duplicate enrolments when attendee email changes in WooCommerce | Course lists show wrong attendee count (PSPO-300326-AB: 11 shown, 8 actual) | OPEN |
| B2 | Orphaned duplicate enrolments 1796, 15, 19 on course 130 | Stale data | OPEN — cleanup needed |

**B1 — ETL duplicate enrolments detail:**

Root cause: `EnrolmentRepository.UpsertAsync` matches by `student_id + order_id + course_schedule_id`. When an email is corrected in WooCommerce, a new student row is created (email is unique key). The upsert can't find the old enrolment (wrong student_id) and inserts a duplicate.

Fix: match existing enrolments by `order_id + course_schedule_id` (not student_id). When found, update the `student_id` on the existing enrolment. Handle multi-ticket orders (e.g. order 12874 had 2 tickets).

Affected files:
- `Bagile.EtlService/Services/WooOrderService.cs:93-111`
- `Bagile.Infrastructure/Repositories/EnrolmentRepository.cs:16-59`

**B2 — Cleanup:** Delete or cancel enrolments 1796, 15, 19 on course 130.

---

### Sprint 31 — "UX Polish"

UX audit completed 15 Apr 2026. Full report: `UX_AUDIT.html` in repo root.

**Quick wins (1 session):**

| # | Item | Size | Status |
|---|------|------|--------|
| U1 | Gateway checklist tick → clickable link to published page | XS | **DONE** |
| U2 | Dashboard: reorder — week strip + at-risk before KPI cards | S | READY |
| U3 | Sidebar: move at-risk badge to /courseschedule | XS | READY |
| U4 | Dashboard: fix "View all" links → /courseschedule?view=list | XS | READY |
| U5 | Remove "draft" from Badge.tsx statusBadge map | XS | READY |
| U6 | Separate "Decision due" legend from filter pills | XS | READY |
| U7 | Side panel: move Edit button to SlideOver header (add actions prop) | S | READY |
| U8 | Promote "Publish →" to a proper Button for unpublished gateways | S | READY |
| U9 | Empty week-strip cells → link to /courseschedule (add + affordance on hover) | S | READY |
| U10 | PageHeader title: text-xl → text-2xl | XS | READY |

**Structural (sprint-sized, plan separately):**
- Dashboard: operator-first layout — KPI cards to Revenue page, dashboard = cockpit
- Sidebar: tighten Course Schedule + Private Courses nav grouping
- Side panel: state-driven layout (Planned → big Publish CTA; Live → enrolment hero)
- Calendar: "Next up" context strip above calendar
- Payment visibility: when Xero re-integrates, payment status in attendee table + side panel

---

## Pull Backlog

Items below are prioritised but not yet scheduled.

### P0 — Private Course Quoting Workflow _(future)_

When an org requests a private course, generate a quote automatically based on:
- Course type (affects trainer rate, certification fees)
- Number of attendees
- Format: virtual vs in-person
- Location: UK vs international (additional travel/logistics cost)

Build as an extension of the existing "Create Private Course" flow — add a "Generate Quote" step before confirming the booking. Output: PDF or email quote with pricing breakdown.

Data model is already ready (`format_type`, `venue_address`, `capacity`, `price` on `course_schedules`). Missing: pricing rules table and quote template.

### P1 — Organisation Data Quality

**Problem:** Widespread org data quality issues from auto-detect matching student email domains instead of billing company domains. Training resellers (NobleProg, QA Ltd etc.) created duplicate entries.

| # | Item | Size | Status |
|---|------|------|--------|
| O1 | `primary_domain` field on organisations table | S | **DONE** (V53) |
| O2 | Seed primary domains for known partners + expand aliases | S | **DONE** (V53, V54) |
| O3 | Organisation merge tool in portal settings | L | Deferred |
| O4 | Fix org list query — alias-based canonical names, filter blanks | M | **DONE** |
| O5 | Organisation detail — show and edit `primaryDomain` + aliases | S | **DONE** |
| O6 | Data cleanup — merge known duplicates (QA ×3, Frazer-Nash ×3, etc.) | M | Partially done (V54) — QA and Frazer-Nash still split |

**Definition of done:** NobleProg shows as one entry with domain `nobleprog.com`, full course history visible.

---

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

### P4 — Technical Health

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

### Sprint 29 — "Course Schedule + Private Courses Polish" (15 Apr 2026)
- [x] Renamed Calendar → Course Schedule throughout; URL `/calendar` → `/courseschedule` (redirect in place)
- [x] `/private-courses` — dedicated Private Courses list page with Create button, badge images, status badges
- [x] API 500 fix: `ArgumentOutOfRangeException` from NULL `start_date` rows — SQL `IS NOT NULL` guard + `SafeAddDays` helper
- [x] APS-SD badge fix: `ExtractCourseType` now handles compound SKU prefixes (e.g. `APS-SD-...` → `APSSD`)
- [x] Gateway fix: private courses correctly show no gateways in both frontend (`calendarHelpers.ts`) and backend (`GatewayConfig.cs`)
- [x] `extractCourseTypeFromSku()` helper — extracts clean type from WooCommerce SKU for badge/display on Private Courses page
- [x] Status field on private course edit — status dropdown in Edit panel; fixes incorrectly-cancelled courses without a migration
- [x] V60: `badge_url` column on `course_definitions` table; seeded for 12 known course types
- [x] `GET /api/course-definitions` + `PATCH /api/course-definitions/{code}/badge` — manage badge URLs via API
- [x] Settings → Courses tab: `CourseDefsEditor` — view all course types, edit badge URLs inline
- [x] `ClientOrganisationName` surfaced on `CourseScheduleItem` (LEFT JOIN org, GROUP BY fix)
- [x] Mobile responsive: table column hiding (`hidden sm/md:table-cell`), header button text wrapping, skeleton grid fix
- [x] `confirmed` status added to private course status badge map
- [x] ETL architecture confirmed safe: WooCommerce upsert conflicts on `(source_system, source_product_id)` — portal courses (`source_system='portal'`) can never be overwritten by WooCommerce ETL

### Sprint 28 — "Unified Course Hub" (14 Apr 2026)
- [x] Add Course modal: Public/Private toggle — Private skips Planned state, creates directly confirmed
- [x] Edit planned course from side panel — reuse modal, pre-fill fields, PATCH on submit
- [x] Trainer filter: load dynamically from DB (`GET /api/trainers`) — no more hardcoded AB/CB
- [x] List view: search + date range filters (Upcoming / year / All)
- [x] Retire `/courses` list — redirect to `/calendar?view=list`; `/courses/[id]` detail pages untouched
- [x] `POST /api/planned-courses/bulk` — per-row validation, partial imports, `{index, success, id?, error?}` response
- [x] CSV import UI: upload → preview table → confirm → bulk create
- [x] CSV export: download all visible courses as CSV
- [x] MCP tool: `create_planned_course` (single + bulk)

### Sprint 26 — "Course Calendar v1" (14 Apr 2026)
- [x] V56: `planned_courses` table — portal-only scheduling intent
- [x] V57: `course_publications` table — gateway publication status per course
- [x] POST/PATCH/DELETE /api/planned-courses — full CRUD for planned courses
- [x] GET /api/calendar — unified feed: planned + WooCommerce courses with gateway status
- [x] POST /api/planned-courses/{id}/publish/ecommerce — creates WooCommerce product (FooEvents meta, Zoom host, PSM-A text fix)
- [x] POST /api/planned-courses/{id}/publish/scrumorg — Playwright automation creates Scrum.org listing
- [x] Course status model: Planned → Partial Live → Live (gateway-driven)
- [x] FullCalendar.js month/week view with course blocks (badge images, status colours, trainer initials)
- [x] Decision deadline `!` indicator, private course `🔒` indicator
- [x] Side panel: gateway checklist, enrolment bar, Go Live actions, external links
- [x] Add planned course modal (12 course types, trainer, dates, virtual/onsite)
- [x] Trainer filter (All / AB / CB), calendar nav in sidebar
- [x] Calendar list view + status filter (follow-up)
- [x] Legacy course live-status fix (follow-up)
- [x] Public schedule API `/api/public/schedule` — partner-facing, excludes private courses, 5-min cache (follow-up)
- [x] Courses page: remove inline calendar toggle, link to /calendar (follow-up)
- [x] V58: `service_config` table — DB-stored credentials for WooCommerce, Scrum.org
- [x] V59: WordPress admin config entry
- [x] Integrations settings tab in portal — manage WooCommerce + Scrum.org credentials from UI
- [x] WooCommerce + Scrum.org secrets wired into CI deploy workflow

### Sprint 25 — "Organisation Data Quality" (13 Apr 2026)
- [x] V53: `primary_domain` column on organisations table, seeded for all known partners
- [x] V54: Organisation consolidation — fix duplicates, add missing orgs
- [x] V55: Backfill 97 historical private course records from Xero
- [x] Expanded aliases for NobleProg, QA Ltd, BAgile, BHF, JISC
- [x] Rewrote `GetOrganisationsAsync` — alias-based canonical name resolution, blank org filtering
- [x] Configure organisation in portal: edit aliases and primary domain
- [x] Dynamic year filter on org list and org detail pages
- [x] Surface backfilled private courses in org course history
- [x] Attendee slide-over on org course history rows

### Sprint 21 extras (2 Apr 2026)
- [x] Trainers table + API + settings UI + dropdown on course panels
- [x] Send Test to Me feature (test email to trainer before sending to attendees)
- [x] Clearer Send dropdown with descriptions
- [x] 5 critical architecture fixes (usage tracking, Xero async, SMTP throw, pageSize cap, webhook null)
- [x] UX quick wins: split info cards, smart primary button, capacity bar, contextual actions, favicon

### Sprint 20 — "Complete the Toolkit" (2 Apr 2026)
- [x] Course Contacts section for private courses (admin/organiser)
- [x] Auto-detect organisation from email domain
- [x] Company as 4th column in paste attendee format
- [x] Inline List/Calendar toggle on courses page
- [x] Format indicator (V/F2F) on calendar tiles
- [x] Template preview pane (Source/Preview toggle)
- [x] CI versioning fix (dev vs latest Docker tags)

### Sprint 19 — "Private Course Polish" (2 Apr 2026)
- [x] Edit private course details (slide-over panel)
- [x] Remove attendee from private course
- [x] Hide Transfer/Refund on private courses
- [x] Simplified attendee table for private (no Org/Country)
- [x] Client organisation parsed from title
- [x] Over-capacity visual warning
- [x] Bug fixes: d-away count, cancelled-as-running (ETL guard), template editor visibility

### Sprint 18 — "Complete the Email Loop" (2 Apr 2026)
- [x] SMTP config for production
- [x] Seed 12 post-course templates (all course types)
- [x] Template editor UI in portal settings
- [x] MCP tools (update_student, send_post_course_email)
- [x] Monitoring API: expose course status

### Sprint 17 — "Calendar Enhancement & Visual Identity" (2 Apr 2026)
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
