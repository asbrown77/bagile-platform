# BAgile Platform — Product Review

> **Last updated:** 26 Mar 2026
> **Reviewed by:** Alex Brown (PO) + Claude (Dev)
> **Purpose:** Living document — extend each session with new findings, decisions, and direction changes.

---

## 1. What Is The Platform?

A .NET 8 API that aggregates data from WooCommerce, Xero, FooEvents, and Stripe into a single queryable source for courses, students, enrolments, orders, organisations, and transfers. Deployed on Hetzner, accessed via MCP tools from Claude.

**Stack:** .NET 8.0, PostgreSQL 16, Dapper, MediatR (CQRS), Clean Architecture
**Production:** https://api.bagile.co.uk
**Repo:** github.com/asbrown77/bagile-platform

---

## 2. Current Capabilities

### API Endpoints (19 total)

| Resource | Read | Write | Notes |
|----------|------|-------|-------|
| Course Schedules | GET list, GET by id, GET attendees, GET monitoring | POST cancel | Monitoring has business logic (minimums, deadlines) |
| Enrolments | GET list | — | Filter by course, student, status, org |
| Orders | GET list, GET by id | — | No payment status (Xero/Stripe missing) |
| Organisations | GET list, GET by name, GET course history | — | Derived from student data, no enrichment |
| Students | GET list, GET by id, GET enrolments | — | 1,975 students in DB |
| Transfers | GET list, GET pending, GET by course | — | Transfer detection via ETL |
| Health | GET /health | — | |
| Webhooks | POST /webhooks/{source} | — | WooCommerce + Xero (HMAC validated) |

### ETL Service

- Background worker running every 5 minutes
- **Active:** WooCommerce collector (orders + products)
- **Disabled:** Xero collector (OAuth token issues, rate limiting)
- **Missing:** Stripe, FooEvents direct, MailChimp

### MCP Server (18 tools)

- 16 read tools wrapping all GET endpoints
- `get_course_monitoring` — business logic layer (minimums, deadlines, urgency)
- `cancel_course` — first write operation
- `apiPost` function ready for more write tools

### Database

- PostgreSQL 16, `bagile` schema
- 25 Flyway migrations
- 10 tables: course_schedules, enrolments, students, orders, order_line_items, organisations, transfers, raw_orders, raw_products, course_definitions

### Tests

| Type | Count | Notes |
|------|-------|-------|
| Unit tests (.NET) | 83 | Handlers, parsers, business logic |
| Integration tests (.NET) | 51 | API endpoints, ETL pipeline (need running DB) |
| Acceptance tests (.NET) | 9 | Broken — need running API, excluded from CI |
| MCP server tests (vitest) | 24 | api-client, formatResult |
| **Total** | **158** (excluding broken acceptance) | |
| **Code coverage** | ~25% line, ~18% branch | Low — infrastructure layer mostly untested |

### CI/CD

- GitHub Actions: build, test, Docker push to GHCR, SSH deploy to Hetzner
- Separate pipelines for API (`api-v*` tags) and ETL (`etl-v*` tags)
- Acceptance tests excluded from CI (always fail without running DB)

---

## 3. Data Quality Issues

| Issue | Severity | Example | Root Cause |
|-------|----------|---------|------------|
| Duplicate course schedules | High | 3x PSMAI entries on 6 May (IDs 985, 986, 983) | ETL upsert conflict key may not catch all duplicates |
| Inconsistent SKUs | Medium | `APSSD` vs `APS-SD` in WooCommerce | Source data inconsistency, affects minimum calculation |
| Null start dates | Medium | Old courses with no start_date | WooCommerce products missing meta fields |
| Empty SKUs | Low | Some courses have empty string instead of null | Parser edge case |
| No payment data | High | Orders exist but payment status unknown | Xero collector disabled, Stripe not integrated |

---

## 4. Architecture Assessment

### What Works Well

- **Clean Architecture** properly enforced — Domain has no dependencies, Application depends only on Domain
- **CQRS via MediatR** — consistent pattern, easy to add new queries/commands
- **Dapper + explicit SQL** — no ORM surprises, full control over queries
- **Monitoring endpoint** — genuine business logic in the right layer (Application), not in SQL
- **MCP server** — thin passthrough, no business logic leak, correct separation
- **ETL worker** — simple, resilient (catches exceptions, continues cycling)
- **Webhook handling** — HMAC-SHA256 signature validation for WooCommerce + Xero

### Architecture Gaps

| Gap | Impact | Effort to Fix |
|-----|--------|---------------|
| Read-only API (95%) | Can't take actions via Claude beyond cancel | Medium per endpoint |
| No Xero data | Payment/invoice status missing | High (OAuth2, token refresh) |
| No Stripe data | Can't verify payments across 2 accounts | Medium |
| No ETL deduplication | Duplicate records in course_schedules | Medium |
| Single ETL source | Only WooCommerce active | High (per source) |
| No event sourcing | Status changes not tracked (just overwritten) | High |
| Organisation table is derived | No persistent enrichment | Medium |

---

## 5. Technical Debt Register

| # | Item | Severity | Effort | Status |
|---|------|----------|--------|--------|
| 1 | ETL deduplication (duplicate course schedules) | High | Medium | Open |
| 2 | Xero collector (payment data missing) | High | High | Open — needs investigation sprint |
| 3 | Data cleanup (nulls, empty SKUs, inconsistent codes) | Medium | Low | Open |
| 4 | Acceptance tests (broken, excluded from CI) | Medium | Medium | Open — fix or delete |
| 5 | Code coverage 25% → target 40-50% | Medium | Ongoing | Open |
| 6 | ETL hardcoded 5-min interval (should be config) | Low | Low | Open |
| 7 | Organisation enrichment (partner type, discounts) | Low | Medium | Open |
| 8 | ~~ETL double delay (10 min not 5)~~ | ~~Medium~~ | — | **Resolved** Sprint 1 |
| 9 | ~~No MCP server~~ | ~~High~~ | — | **Resolved** Sprint 1 |
| 10 | ~~APS-SD SKU parser bug~~ | ~~Low~~ | — | **Resolved** Sprint 3 |

---

## 6. Product Direction — Open Question

The platform does **one thing well**: aggregate WooCommerce data into a queryable API with a monitoring layer.

But the "golden source" vision is incomplete. Two possible directions:

### Direction A: Complete the Data
Fix Xero, add Stripe, clean duplicates, enrich organisations. Result: truly single source of truth, but still mostly read-only.

### Direction B: Add Actions
More write endpoints (transfers, enrolment updates, lead management). Result: Claude can do things, but underlying data may still be incomplete.

**Decision needed:** Which direction delivers more value for a 2-person training company?

**Arguments for A:** You can't make good decisions on incomplete data. Cancel decisions need payment status. Lead tracking needs org context.

**Arguments for B:** The monitoring endpoint proved that actionable intelligence is more valuable than complete data. 95% of the time you need to *do something*, not just *see something*.

**Current leaning (26 Mar 2026):** Direction A first (Xero investigation), then B. Rationale: payment visibility unblocks better cancel decisions, which is the #1 daily operation.

---

## 7. Sprint History

### Sprint 1 (26 Mar 2026)
- [x] Fix ETL double delay (10 min → 5 min)
- [x] Build MCP server (18 tools wrapping all endpoints)
- [x] Find and configure production API key
- **Outcome:** Claude can read all platform data

### Sprint 2 (26 Mar 2026)
- [x] Course monitoring endpoint with business logic
- [x] Cancel course endpoint (first write operation)
- [x] MCP tools for monitoring + cancel
- **Outcome:** Claude can flag problems and take action

### Sprint 3 (26 Mar 2026)
- [x] Fix APS-SD SKU parser bug
- [x] Fix CI (exclude broken acceptance tests)
- [x] Fix GHCR auth on Hetzner
- **Outcome:** Clean deployments, correct business logic

### Sprint 4 (Planned)
- [ ] Xero investigation — map OAuth flow, test single invoice pull
- [ ] Decision: fix collector or build simpler approach
- **Goal:** Understand what's broken before committing to a fix

---

## 8. Decisions Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 26 Mar 2026 | Leads table deferred | Evaluate CRM tools (HubSpot Free etc.) before building custom. Trello works for now. |
| 26 Mar 2026 | Course monitoring before Xero | Daily cancel decisions are #1 pain, monitoring is buildable in 1 session |
| 26 Mar 2026 | MCP server in same repo | Thin wrapper on platform API, tightly coupled to endpoints |
| 26 Mar 2026 | Agent/Trello tools in separate folder | Platform repo is for platform code only, agent tools go to b-agile/agent/ |
| 26 Mar 2026 | SSH deploy as fallback | GHCR PAT expired, fixed later. CI/CD via tags is primary method. |

---

## 9. Next Review Actions

- [ ] Investigate ETL deduplication — are the 3 duplicate PSMAI entries a one-off or systemic?
- [ ] Decide Direction A vs B based on next session's priorities
- [ ] Review Xero OAuth flow before committing to a sprint
- [ ] Assess whether acceptance tests should be fixed or removed
- [ ] Review code coverage — identify highest-value areas to test next
