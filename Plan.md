# BAgile Platform — Plan

> **Goal:** Complete the golden source so all BAgile business data is in one place, queryable by Claude via MCP, enabling automated course management, email triage, and lead tracking.

## Phase 1: Golden Source Completion

### P1 — Critical (Unblocks everything)

#### 1.1 Production API Key & Access ✓
- [x] Find/create production API key for api.bagile.co.uk
- [x] Verify all existing endpoints work in production
- [x] Document in .env and CLAUDE.md
- **Why:** Can't use the platform as golden source if we can't query it
- **Files:** Bagile.Api/appsettings.json, GitHub Actions secrets

#### 1.2 MCP Server for BAgile API ✓
- [x] Create MCP server that wraps the BAgile API endpoints
- [x] Tools: query courses, students, enrolments, orders, organisations, transfers
- [x] Configure in Claude Code settings so available as a tool
- **Why:** Claude needs to query the golden source directly, not via scripts
- **Files:** New project — consider TypeScript MCP server or .NET
- **Reference:** Claude MCP documentation

#### 1.3 Re-enable Xero Collector
- [ ] Uncomment XeroCollector in EtlService/Program.cs
- [ ] Fix known issues: rate limiting (429), token refresh, 404 handling
- [ ] Test with production Xero (tenant: aef46d85)
- [ ] Verify invoice status flows into orders (payment reconciliation)
- **Why:** Payment data is missing from golden source — we had to query Xero separately today
- **Files:** Bagile.EtlService/Program.cs, Bagile.Infrastructure/Clients/XeroApiClient.cs

#### 1.4 Fix ETL Double Delay ✓
- [x] Remove duplicate 5-minute delay in EtlWorker.cs (lines 46-47)
- [x] Confirm ETL runs every 5 mins not 10
- **Why:** Data freshness — 10 min delay means stale enrollment counts
- **Files:** Bagile.EtlService/Services/EtlWorker.cs

### P2 — High (Core missing features)

#### 2.1 Leads Table & Endpoints
- [ ] Design leads schema: company, contact_name, contact_email, course_interest, source, status, estimated_value, next_action_date, notes, created_at, updated_at
- [ ] Create migration V26__create_leads_table.sql
- [ ] Add Lead entity, repository, query handler
- [ ] Add LeadsController with CRUD endpoints
- [ ] ETL: optionally sync from Trello or manual entry
- **Why:** Leads are currently in Trello with no connection to the rest of the data
- **Files:** New migration, new entity, new controller

#### 2.2 Organisation Enrichment
- [ ] Add fields: partner_type (PTN/preferred_supplier/direct), discount_rate, discount_coupon, primary_contact_name, primary_contact_email, billing_notes, scrum_org_id
- [ ] Create migration V27__enrich_organisations.sql
- [ ] Update OrganisationsController to return enriched data
- **Why:** When OTP Banka emails, we should instantly see their discount rate, contact person, and history
- **Files:** Migration, Bagile.Domain/Entities, Bagile.Infrastructure/Persistence/Queries/OrganisationQueries.cs

#### 2.3 Write Endpoints
- [ ] POST /api/leads — create lead
- [ ] PUT /api/leads/{id} — update lead status
- [ ] POST /api/course-schedules/{id}/cancel — cancel a course
- [ ] POST /api/enrolments/{id}/transfer — initiate transfer
- **Why:** Currently read-only API — can't take action, only observe
- **Files:** New commands in Bagile.Application, new controller methods

### P3 — Medium (Automation enablers)

#### 3.1 Course Monitoring Endpoint
- [ ] GET /api/course-schedules/monitoring — returns courses with enrollment counts vs minimums
- [ ] Include decision deadline based on course day of week
- [ ] Flag courses below minimum with recommended action
- **Why:** Enables the Course Manager agent to check daily
- **Rules:**
  - Standard courses (PSM, PSPO, PSK etc.): min 3
  - Interactive courses (PSM-A, PSFS, APS, APS-SD): min 4
  - Decision timing: Mon/Tue course → decide previous Fri. Wed-Fri → decide 2 days before.
  - If 1 enrolled on popular course → wait over weekend. If quiet course → call early.

#### 3.2 Stripe Payment Integration
- [ ] Add Stripe payment status to orders (check both Website and Xero accounts)
- [ ] Store payment_intent_id and charge_status on orders
- [ ] Surface in API: GET /api/orders/{id} includes payment status
- **Why:** Currently have to query Stripe separately to check payment

#### 3.3 Communication Log
- [ ] Design schema for linking emails/notes to organisations and leads
- [ ] Optional: Gmail message IDs linked to lead/organisation records
- **Why:** "What's the last thing we said to OTP Banka?" shouldn't require searching Gmail

### P4 — Future (Nice to have)

#### 4.1 MailChimp Integration
- [ ] Sync past students to MailChimp segments automatically
- [ ] Track which campaigns were sent to which contacts

#### 4.2 Scrum.org Sync
- [ ] Playwright automation to sync course listings between WordPress and Scrum.org
- [ ] Assessment score import

#### 4.3 Dashboard/Frontend
- [ ] Simple web UI for the platform (optional — Claude via MCP may be sufficient)

---

## Sprint Queue

### Sprint 4: Xero Investigation
> Understand what's broken before trying to fix it
- [ ] Review XeroApiClient.cs — map OAuth flow, token refresh, rate limiting
- [ ] Check Xero collector logs from last time it was enabled
- [ ] Spike: single invoice query without the collector
- [ ] Decision gate: fix collector OR simpler Xero read-only approach

### Sprint 5+: Future
- [ ] Stripe payment status on orders
- [ ] Organisation enrichment (partner_type, discount_rate)
- [ ] MailChimp integration
- [ ] Scrum.org sync

### Deferred (decision pending)
- **Leads table** — deferred until CRM tool decision (HubSpot Free vs custom). Trello works for now.
- **Communication log** — evaluate as part of CRM decision

## Completed

### Sprint 3 (26 Mar 2026)
- [x] **Fix APS-SD SKU parser** — multi-segment course codes now parsed correctly
- [x] **Fix CI** — exclude acceptance tests (need running DB, always fail in CI)
- [x] **GHCR auth** — identified expired PAT, SSH deploy works as fallback
- [x] **Separation** — Trello/agent tools moved to agent folder, platform repo stays focused

### Sprint 2 (26 Mar 2026)
- [x] **Course Monitoring Endpoint** — `GET /api/course-schedules/monitoring` with enrolment vs minimums, decision deadlines, urgency ranking. 33 unit tests.
- [x] **Cancel Course Endpoint** — `POST /api/course-schedules/{id}/cancel` — first write endpoint. Idempotent.
- [x] **MCP tools** — `get_course_monitoring` + `cancel_course` added. `apiPost` for write operations.
- [x] **Deployed** via SSH to Hetzner. Monitoring returning live data.

### Sprint 1 (26 Mar 2026)
- [x] **1.4 Fix ETL Double Delay** — Removed duplicate Task.Delay in EtlWorker.cs. Added regression test.
- [x] **1.2 MCP Server for BAgile API** — TypeScript MCP server (bagile-mcp-server/) with 18 tools wrapping all API endpoints. 24 unit tests.
- [x] **1.1 Production API Key** — Found and configured
