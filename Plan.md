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

*All sprints complete. Next: dashboard UI for cancel/transfer + analytics pages.*

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

## Pull Backlog

Items below are prioritised but not yet scheduled. Refine before pulling into a sprint.

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

### Deferred

- **Leads table** — deferred until CRM tool decision (HubSpot Free vs custom). Trello works for now.
- **Communication log** — evaluate as part of CRM decision.
- **Zoom integration** — need to investigate where Zoom details are stored (WooCommerce product meta?)

---

## Completed

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
