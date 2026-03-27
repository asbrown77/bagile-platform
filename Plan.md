# BAgile Platform — Plan

> **Vision:** The golden source for BAgile Ltd's training business — all course, student, enrolment, order, organisation, and payment data in one place, queryable and actionable by Claude via MCP.

---

## Pull Backlog

Items below are prioritised but unrefined. Refine before pulling into a sprint.

### P1 — Trust the Data

| ID | Item | Who Needs It | Status |
|----|------|-------------|--------|
| D1 | ETL deduplication — duplicate course schedules | Platform reliability | Ready to refine |
| D2 | Data cleanup migration — nulls, empty SKUs, inconsistent codes | Monitoring accuracy | Ready to refine |
| D3 | Delete or fix acceptance tests | CI integrity | Ready to refine |
| D4 | ETL interval to config (appsettings) | Ops flexibility | Ready to refine |

### P2 — Complete the Picture

| ID | Item | Who Needs It | Status |
|----|------|-------------|--------|
| X1 | Xero investigation — map OAuth, spike single invoice | Payment visibility | Ready to refine |
| X2 | Re-enable Xero collector (or new approach) | Payment visibility | Blocked by X1 |
| X3 | Add payment_status to orders | Daily operations | Blocked by X2 |
| S1 | Stripe payment check endpoint (both accounts) | Daily operations | Ready to refine |

### P3 — Make It Actionable

| ID | Item | Who Needs It | Status |
|----|------|-------------|--------|
| W1 | POST /api/enrolments/{id}/transfer | Course management | Ready to refine |
| W2 | PUT /api/organisations/{name} — enrichment updates | Org data | Ready to refine |
| A1 | GET /api/course-schedules/summary — dashboard stats | Quick visibility | Ready to refine |
| A2 | GET /api/analytics/fill-rates — historical by course type | Scheduling decisions | Ready to refine |
| A3 | GET /api/analytics/booking-lead-times | Cancellation timing | Ready to refine |

### P4 — Enrich & Extend

| ID | Item | Who Needs It | Status |
|----|------|-------------|--------|
| O1 | Organisation enrichment migration (partner_type, discount_rate) | Pricing queries | Ready to refine |
| O2 | Seed known partner data (QA=PTN33, etc.) | Immediate value | Blocked by O1 |

### P5 — Future

| ID | Item | Status |
|----|------|--------|
| M1 | MailChimp integration | Deferred |
| SC1 | Scrum.org sync (Playwright) | Deferred |
| UI1 | Dashboard/Frontend (may not need — MCP sufficient) | Deferred |

### Deferred (decision pending)

- **Leads table** — deferred until CRM tool decision (HubSpot Free vs custom). Trello works for now.
- **Communication log** — evaluate as part of CRM decision.

---

## Sprint Queue

### Sprint 4: Data Integrity → Sprint 4

**Goal:** The monitoring endpoint returns trustworthy data — no duplicates, no wrong minimums, no ghost courses.
**We'll know it worked when:** `get_course_monitoring` returns exactly the right number of unique courses for the next 14 days, each with correct SKU and minimum.

**Refined PBIs:**

#### D1: Fix ETL Deduplication
- **What:** Investigate and fix duplicate course schedules created by ETL (e.g. 3x PSMAI on 6 May)
- **Who needs it:** Anyone using monitoring — duplicates inflate course counts and confuse cancel decisions
- **Today:** ETL upsert may create duplicates when WooCommerce product IDs change or when SKU matching fails
- **Should be:** One course schedule per unique course instance. Upsert on (source_system, source_product_id) is unique but same course can arrive with different product IDs
- **Data source:** `bagile.course_schedules` table, ETL `CourseScheduleRepository.UpsertAsync`
- **Files likely touched:** `CourseScheduleRepository.cs`, `WooProductParser.cs`, possibly V26 migration for cleanup
- **Acceptance criteria:**
  - [ ] Root cause identified for the 3 duplicate PSMAI entries
  - [ ] Fix prevents future duplicates
  - [ ] V26 migration cleans up existing duplicates
  - [ ] Unit test for the dedup logic

#### D2: Data Cleanup Migration
- **What:** Fix null start dates, empty-string SKUs, inconsistent codes (APSSD vs APS-SD)
- **Who needs it:** Monitoring endpoint — SKU parsing drives minimum calculation
- **Today:** Some courses have `sku = ''` instead of NULL, `APSSD` instead of `APS-SD`
- **Should be:** All courses have proper SKUs matching the pattern `COURSECODE-DDMMYY-TRAINER`
- **Data source:** `bagile.course_schedules` table
- **Files likely touched:** New migration V26 or V27, possibly `WooProductParser.cs`
- **Acceptance criteria:**
  - [ ] Empty-string SKUs set to NULL
  - [ ] `APSSD` standardised to `APS-SD` pattern
  - [ ] Courses with null start_date identified (report, don't delete)

#### D3: Fix or Delete Acceptance Tests
- **What:** The 9 acceptance tests always fail in CI (need running API+DB). Either wire them to a test container or delete them.
- **Who needs it:** CI pipeline integrity — green pipeline should mean green
- **Today:** Tests excluded via filter, dead code in repo
- **Should be:** Either tests that run in CI (via docker-compose test setup) or removed entirely
- **Files likely touched:** `Bagile.AcceptanceTests/`, possibly `docker-compose.test.yml`
- **Acceptance criteria:**
  - [ ] Decision: fix or delete
  - [ ] If fix: tests pass in CI
  - [ ] If delete: project removed, CI simplified

#### D4: ETL Interval to Config
- **What:** Move hardcoded 5-min delay to `appsettings.json`
- **Who needs it:** Ops — different intervals for dev vs prod
- **Today:** `TimeSpan.FromMinutes(5)` hardcoded in EtlWorker.cs:46
- **Should be:** `_options.IntervalMinutes` from config
- **Files likely touched:** `EtlWorker.cs`, `appsettings.json`, new `EtlOptions.cs`
- **Acceptance criteria:**
  - [ ] Interval read from config
  - [ ] Default 5 min if not specified
  - [ ] Existing regression test still passes

---

## Completed

### Sprint 3 (26 Mar 2026)
- [x] **Fix APS-SD SKU parser** — multi-segment course codes now parsed correctly
- [x] **Fix CI** — exclude acceptance tests (need running DB, always fail in CI)
- [x] **GHCR auth** — identified expired PAT, fixed with new PAT
- [x] **Separation** — Trello/agent tools moved to agent folder, platform repo stays focused

### Sprint 2 (26 Mar 2026)
- [x] **Course Monitoring Endpoint** — `GET /api/course-schedules/monitoring` with enrolment vs minimums, decision deadlines, urgency ranking. 33 unit tests.
- [x] **Cancel Course Endpoint** — `POST /api/course-schedules/{id}/cancel` — first write endpoint. Idempotent.
- [x] **MCP tools** — `get_course_monitoring` + `cancel_course` added. `apiPost` for write operations.
- [x] **Deployed** via SSH to Hetzner. Monitoring returning live data.

### Sprint 1 (26 Mar 2026)
- [x] **Fix ETL Double Delay** — Removed duplicate Task.Delay in EtlWorker.cs. Regression test added.
- [x] **MCP Server for BAgile API** — TypeScript MCP server with 18 tools. 24 unit tests.
- [x] **Production API Key** — Found and configured.
