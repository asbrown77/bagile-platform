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

*All sprints through 26 complete. Sprint 21 pre-course email compose flow built and tested (Bristol PSM 27 Apr ✓).*

---

### Sprint 28 — "Unified Course Hub"
**Goal:** The calendar becomes the single place for all course management. Plan, draft, publish, and import courses — public and private — without navigating to a second page.

**We'll know it worked when:** Alex opens `/calendar`, creates a new PSM planned course, edits the date, publishes to WooCommerce in one click, adds a private course from the same screen, and imports a batch of planned courses from a CSV — all without visiting `/courses`.

| # | Item | Size | Status |
|---|------|------|--------|
| **Create & Edit** | | | |
| 1 | Add Course modal: Public/Private toggle — Private skips Planned state, creates directly live | S | DONE |
| 2 | Edit planned course from side panel — reuse modal, pre-fill fields, PATCH on submit | S | DONE |
| 3 | Trainer filter: load dynamically from DB instead of hardcoded AB/CB strings | XS | DONE |
| **List view polish** | | | |
| 4 | List view: add search + date range filters (parity with Courses page) | S | DONE |
| 5 | Retire `/courses` list — redirect to `/calendar?view=list`, preserve query params | M | DONE |
| **Import / Export** | | | |
| 6 | `POST /api/planned-courses/bulk` — accept array, validate per-row, return per-row result | S | DONE |
| 7 | CSV import UI: upload → preview table → confirm → bulk create | M | DONE |
| 8 | CSV export: download all planned + live courses as CSV | S | DONE |
| 9 | MCP tool: `create_planned_course` (single and bulk, callable from Claude) | S | DONE |

**CSV format:** `courseType, startDate (YYYY-MM-DD), endDate, trainer, isVirtual (true/false), venue, notes` — one header row, one course per row.

### 3 Amigos — Unified Course Hub

**Tester:**
- Creating a private course from Calendar must behave identically to old Courses page flow
- Editing a planned course must block courseType change if any gateway is already published
- CSV import: bad rows rejected with per-row errors, preview shown before committing
- Redirect from `/courses` must preserve query params (e.g. `/courses?type=PSM` → `/calendar?type=PSM`)
- Trainer filter must update automatically when a trainer is added in Settings

**Dev:**
- Private course creation: call existing private course endpoint (not planned-courses) — same logic, moved into Add Course modal
- Edit modal: reuse `AddCourseModal`, pass existing values as props, switch POST → PATCH on submit
- Bulk endpoint: validate each row individually, partial imports allowed (return array of `{index, success, error}`)
- Redirect: Next.js `redirect()` in `/courses/page.tsx`, not a link

**PO:**
- The word is **Planned** — not "draft". Keep consistent everywhere in UI copy.
- Private courses skip Planned state — they go straight to live (pre-confirmed bookings)
- The `/courses/[id]` detail pages stay untouched — only the list is retired
- MCP bulk create is for importing a trainer's drafted schedule (e.g. Chris sends a spreadsheet)

---

### Sprint 27 — "Course Calendar v2 + Gateway Config" _(deferred)_
_After v1 is live and used for a full month._

- Course types settings page — list of all course types, configure applicable gateways per type
- Internal BAgile course type support
- IC Agile gateway publish automation
- Drag-to-reschedule for planned courses
- Monthly revenue total in calendar footer
- List view (table) for dense weeks ← already done as part of Sprint 26 follow-up
- Mobile responsive calendar

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

## Pull Backlog

Items below are prioritised but not yet scheduled.

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
