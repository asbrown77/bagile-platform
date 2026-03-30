# BAgile Platform — Plan

> **Vision:** The golden source for BAgile Ltd's training business — all course, student, enrolment, order, organisation, and payment data in one place, queryable and actionable by Claude via MCP.

---

## Pull Backlog

Items below are prioritised but unrefined. Refine before pulling into a sprint.

### P1 — Trust the Data (Complete)

All P1 items done across Sprints 4-6. Data is trustworthy, orders sync, multi-ticket enrolments work.

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

---

## Completed

### Sprint 6 (30 Mar 2026)
- [x] **Multi-ticket enrolments** — student per ticket, not per order. V27 migration re-processed existing orders. Order 12902 now shows all 5 attendees.
- [x] **cancel_course 500** — was already fixed in Sprint 4 (DI registration). Verified working in prod.

### Sprint 5 (30 Mar 2026)
- [x] **D5: Order lookup by WooCommerce ID** — queries by both internal ID and external_id. MCP `get_order` works with Woo order numbers.
- [x] **D6: ETL order status sync** — `modified_after` + `date_modified` from payload + 30-day lookback. All 28 orders verified matching WooCommerce.

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
