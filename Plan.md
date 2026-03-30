# BAgile Platform — Plan

> **Vision:** The golden source for BAgile Ltd's training business — all course, student, enrolment, order, organisation, and payment data in one place, queryable and actionable by Claude via MCP and the BAgile Dashboard.

---

## Sprint Queue

*No active sprints — all caught up.*

---

## Pull Backlog

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

### Deferred

- **Leads table** — deferred until CRM tool decision (HubSpot Free vs custom). Trello works for now.
- **Communication log** — evaluate as part of CRM decision.

---

## Completed

### Sprint 10 (31 Mar 2026)
- [x] Revenue summary endpoint + dashboard cards (£ this month, £ this year, monthly breakdown)

### Sprint 9 (31 Mar 2026)
- [x] Cancel + transfer workflow API: mark refund, mark transfer, transfer to course, cancel with actions
- [x] MCP tools for transfers
- [x] V30 migration: cancellation_reason, pending_transfer index

### Sprint 8 (31 Mar 2026)
- [x] Scrum.org CSV export (First Name, Last Name, Email, Country)
- [x] V29 migration: country column on students
- [x] ETL extracts billing_country from WooCommerce
- [x] Filename format: PSPO-Students-300326.csv

### Sprint 7 (30 Mar 2026)
- [x] Removed automatic transfer heuristic — transfers now explicit via dashboard/MCP
- [x] Fixed PSPO 30-31 Mar: 8 correct attendees verified against WooCommerce export

### Sprint 6 (30 Mar 2026)
- [x] Multi-ticket enrolments, cancel_course verified, portal + dashboard MVP, API key management, MCP standalone repo

### Sprint 5 (30 Mar 2026)
- [x] D5: Order lookup by WooCommerce ID. D6: ETL order status sync.

### Sprint 4 (27 Mar 2026)
- [x] D1-D4: Deduplication, data cleanup, dead tests, ETL interval config

### Sprint 3 (26 Mar 2026)
- [x] APS-SD parser fix, CI fix, GHCR auth, repo separation

### Sprint 2 (26 Mar 2026)
- [x] Course monitoring endpoint, cancel course endpoint, MCP tools, deployed

### Sprint 1 (26 Mar 2026)
- [x] ETL double delay fix, MCP server, production API key
