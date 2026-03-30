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
| D5 | ~~GET /orders/{id} returns 404 for WooCommerce order numbers~~ | MCP usability, daily ops | **Done — Sprint 5** |
| D6 | ~~ETL not syncing order status updates~~ | Data accuracy, monitoring, revenue reporting | **Done — Sprint 5** |

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

### Sprint 5: Data Trust Bugs (30 Mar 2026)

**Goal:** Orders are queryable by WooCommerce order number and status changes sync automatically.
**We'll know it worked when:** `get_order 12912` returns the order, and order status updates from WooCommerce appear within one ETL cycle.

| # | Item | Status |
|---|------|--------|
| 1 | D5: Fix order lookup by WooCommerce order number | Done |
| 2 | D6: Fix ETL order status sync | Done |

#### D5: Fix — Order lookup now supports WooCommerce order numbers
- `OrderQueries.GetOrderByIdAsync` queries by both `o.id` and `o.external_id`, internal ID takes priority
- Enrolment sub-query uses actual internal ID from result, not the passed-in parameter
- 2 integration tests added (external ID lookup + internal ID priority)

#### D6: Fix — ETL now syncs order status updates (2 root causes)
1. `RawOrderRepository.GetLastTimestampAsync` was returning `MAX(created_at)` (local DB timestamp) → now extracts `date_modified` from WooCommerce JSON payload
2. `WooApiClient.FetchOrdersAsync` was using `after=` (filters by creation date) → now uses `modified_after=` (filters by modification date)
- 2 unit tests added (new order upsert + re-processed order status update)

---

### Sprint 6: Multi-Ticket Enrolments (30 Mar 2026)

**Goal:** Multi-ticket orders create one enrolment per attendee, not one per order.
**We'll know it worked when:** Order 12874 shows 2 enrolments (Olorundurotimi + Maxwell) and order 12902 shows 5 enrolments.

| # | Item | Status |
|---|------|--------|
| 1 | Fix WooOrderService to create student per ticket | Done |
| 2 | Verify cancel_course 500 (was fixed Sprint 4) | Done — working in prod |

#### Fix: Student creation moved inside enrolment loop
- **Root cause:** `WooOrderService.ProcessAsync` created ONE student from the first ticket, then used the same `studentId` for all enrolments. Since `EnrolmentRepository.UpsertAsync` deduplicates on `(student_id, order_id, course_schedule_id)`, all tickets collapsed into 1 enrolment.
- **Fix:** Extracted `CreateStudentFromTicketOrBillingAsync()` helper. Each ticket in the foreach loop now creates/upserts its own student before creating the enrolment.
- **FooEvents API confirmed working** — returns correct attendee data for both orders.

---

## Completed

### Sprint 4 (27 Mar 2026)
- [x] **D1-D4:** Deduplication fix, data cleanup migration, dead test removal, ETL interval to config

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
