# BAgile Platform — Product Vision & Roadmap

## Product Vision

> **For** Alex and Chris at BAgile Ltd,
> **who** run a Scrum/Agile training business with 100+ public courses per year,
> **the BAgile Platform** is a golden source and AI operations layer
> **that** gives instant visibility into courses, bookings, payments, leads, and organisations,
> **unlike** the current fragmented setup across WooCommerce, Xero, Stripe, Trello, Google Sheets, and email,
> **our product** enables Claude to act as a Course Manager, PA, and CRM — making decisions, not just showing data.

## Business Context (Mar 2026)

- **Revenue model:** Public courses (£550-£1,495/seat) + private/enterprise training
- **Team:** Alex Brown + Chris Bexon (trainers), no admin staff
- **Scale:** ~104 courses planned Q3/Q4 2026, £158-250K realistic revenue
- **Pain points:**
  1. 27 of 68 scheduled courses have ZERO enrolments — need daily go/cancel decisions
  2. 6 active leads worth £50K+ stuck in Trello, no connection to course/payment data
  3. Can't check payment status without manually querying Xero + 2 Stripe accounts
  4. No marketing campaigns sent in 2+ months — 1,970 subscribers sitting idle
  5. Two-person team means every manual check costs training prep time

## Strategic Goals

| # | Goal | Metric | Target |
|---|------|--------|--------|
| G1 | Never miss a cancellation deadline | Courses cancelled < 3 days before start | 0 |
| G2 | Close pipeline leads faster | Lead → decision time | < 5 business days |
| G3 | See payment status instantly | Manual Xero/Stripe lookups per week | 0 |
| G4 | Keep seats filled | Courses running below minimum | < 10% |
| G5 | Re-engage past students | MailChimp campaigns per month | ≥ 2 |

## Roadmap

### Phase 1: See Everything (Sprint 1 ✓)
_"Can Claude read all our data?"_

- [x] MCP server — 16 tools wrapping all API endpoints
- [x] ETL running at correct 5-min intervals
- [x] Production API key found and configured

### Phase 2: Act on Courses (Sprint 2 — NOW)
_"Claude tells me which courses to cancel and when"_

- [ ] **Course monitoring endpoint** — enrollment vs minimums, decision deadlines, recommended actions
- [ ] **Write endpoint: cancel course** — mark course as cancelled via API
- [ ] **MCP tools for monitoring + cancellation** — Claude can check daily and draft recommendations

**Why this is next:** 27 courses with zero enrolment. Every day without monitoring = risk of running empty courses or cancelling too late. This is the #1 daily operational pain.

### Phase 3: Know Your Money (Sprint 3)
_"Is this order paid? What's our revenue this month?"_

- [ ] **Re-enable Xero collector** — invoices, payments, contacts back in golden source
- [ ] **Stripe payment status on orders** — check both Website and Xero accounts
- [ ] **Revenue summary endpoint** — booked vs paid vs outstanding

**Why:** Payment queries are the #2 daily time sink. Also unblocks correct invoicing (PTN33 coupon validation).

### Phase 4: Manage the Pipeline (Sprint 4)
_"Claude tracks leads from email to booking"_

- [ ] **Leads table + CRUD endpoints** — schema, migration, controller
- [ ] **Organisation enrichment** — partner type, discount rate, primary contact
- [ ] **MCP write tools for leads** — create, update status, add notes
- [ ] **Trello → Leads migration** — import 6 active leads, archive stale

**Why:** 6 active leads worth £50K+ are in Trello with no connection to course/payment data. But courses come first — no point closing leads if courses get cancelled.

### Phase 5: Automate Outreach (Sprint 5+)
_"Claude sends the right message to the right people"_

- [ ] MailChimp integration — sync past students to segments
- [ ] Communication log — link emails to orgs/leads
- [ ] Scrum.org sync — Playwright automation for course listings

---

## Sprint 2 Plan

**Goal:** Claude can monitor course health daily and recommend cancellations.

**Duration:** 1 session

### Stories

#### S2.1: Course Monitoring Endpoint
**As** a trainer, **I want** to see which courses need attention, **so that** I can make go/cancel decisions before the deadline.

**Acceptance Criteria:**
- `GET /api/course-schedules/monitoring` returns all courses in next 30 days
- Each course shows: enrolment count, minimum required, fill %, status (healthy/at_risk/critical/cancelled)
- Decision deadline calculated: Mon/Tue course → previous Fri. Wed-Fri → 2 days before.
- Minimums: standard courses = 3, interactive (PSM-A, PSFS, APS, APS-SD) = 4
- Sorted by decision deadline (most urgent first)

#### S2.2: Cancel Course Endpoint
**As** a trainer, **I want** to cancel a course via the API, **so that** Claude can execute cancellations after I approve.

**Acceptance Criteria:**
- `POST /api/course-schedules/{id}/cancel` with reason
- Updates course status in database
- Returns updated course details
- Idempotent — cancelling already-cancelled course is a no-op

#### S2.3: MCP Tools for Monitoring
**As** Claude, **I need** monitoring and cancellation tools, **so that** I can check course health and take action.

**Acceptance Criteria:**
- `get_course_monitoring` tool added to MCP server
- `cancel_course` tool added to MCP server
- Both tools work end-to-end against production API

### Definition of Done
- All endpoints have unit + integration tests
- MCP server rebuilt with new tools
- Deployed to production
- Claude can call `get_course_monitoring` and get actionable data
