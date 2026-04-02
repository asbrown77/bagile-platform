# BAgile Platform — Progress & Lessons Learned

## Current State (26 Mar 2026)
- **API:** Live at api.bagile.co.uk, healthy, 17 endpoints, read-only
- **ETL:** Running on Hetzner, WooCommerce collector active, Xero collector disabled
- **Database:** PostgreSQL 16, 25 migrations, 10 tables
- **Tests:** 109 passing (50 unit + 9 acceptance + 50 integration) + 24 MCP server tests
- **MCP:** Built — 16 tools wrapping all API endpoints (bagile-mcp-server/)
- **Version:** Unknown — need to check tags

## Technical Debt

| Item | Severity | Notes |
|------|----------|-------|
| Xero collector disabled | High | Payment data missing from golden source |
| ~~ETL double delay (10 min not 5)~~ | ~~Medium~~ | **RESOLVED** — removed duplicate Task.Delay, regression test added |
| ETL no backoff on failure | Medium | EtlWorker.cs:40-43 -- fixed 5-min retry even when external APIs are down |
| ETL hardcoded interval | Low | EtlWorker.cs:46 -- 5-min interval should be in config, not hardcoded |
| No write endpoints | High | Can't create leads, update enrolments, or cancel courses via API |
| ~~No MCP server~~ | ~~High~~ | **RESOLVED** — bagile-mcp-server/ with 16 tools, configured in .claude/settings.local.json |
| ~~Production API key unknown~~ | ~~Critical~~ | **RESOLVED** — key found and configured in MCP server |
| APS-SD SKU parser bug | Low | ExtractBaseCourseCode returns "APS" not "APS-SD" — "SD" mis-identified as trainer initials. Minimum is still correct (APS is also interactive). Fix: allowlist multi-segment codes or require trainer suffix to be ≥3 chars. GetCourseMonitoringQueryHandler.cs:94-96 |
| No leads table | High | CRM data not in golden source |
| Organisations table is virtual/derived | Medium | No persistent enrichment (discount rate, partner type) |

## Lessons Learned

### Session 26 Mar 2026 — Initial Setup

**Data Verification:**
- Never state payment status without checking ALL systems: WooCommerce, Stripe (both accounts), Xero invoices
- BACS payments won't show in Stripe — only in Xero when reconciled
- Invoice amounts may differ from order amounts (partner discounts, VAT, currency)

**Email Handling:**
- Always read full thread (sent + received) before acting
- Always check sent folder — drafts may have been sent
- Don't create duplicate drafts on same thread

**QA Partner Rate:**
- Current rate: PTN33 (33%) since Sep 2025
- Common issue: QA books without coupon → invoice at RRP → finance queries it
- Consider: n8n automation to check coupon on QA orders before invoicing

**FooEvents:**
- Transfers require order "completed" (paid) status
- Process: cancel old ticket → create new ticket on destination course → copy details → submit → resend
- Resend button only appears when order is completed

**Platform:**
- api.bagile.co.uk is live and healthy but we don't have the prod API key
- Xero OAuth tokens expire in 30 mins — need refresh mechanism built into any script
- The n8n mailroom workflows are sub-workflows (executeWorkflowTrigger) — can't call directly from API

## Sprint 16 (1 Apr 2026) — Follow-Up Emails & Attendee Override

### What was built (4 commits)

**Commit 1 — DB migrations**
- V37: `bagile.post_course_templates` table with PSPO seed template
- V38: `overridden_fields` (JSONB), `updated_by`, `override_note` on `students` table

**Commit 2 — Email service**
- `IEmailService` interface + `SmtpEmailService` implementation
- Reuses `Smtp:*` config section. Added `Smtp:Port` (default 587).
- Throws on send failure — surfaces 500 to API caller rather than silent loss.

**Commit 3 — Post-course templates**
- Full CQRS stack: entity, repository, application commands/queries, TemplatesController
- `GET /api/templates/post-course` — list all
- `GET /api/templates/post-course/{courseType}` — get one
- `PUT /api/templates/post-course/{courseType}` — create/update
- `POST /api/templates/post-course/send/{courseScheduleId}` — send follow-up
- Template variables: `{{greeting}}`, `{{trainer_name}}`, `{{course_dates}}`, `{{delay_note}}`
- Course type derived from code prefix (PSPO-300326-CB → PSPO)
- Sends to all active attendees, CC info@bagile.co.uk always
- Portal: `SendFollowUpPanel` slide-over with recipient list, preview, delay note, type override

**Commit 4 — Attendee override (P1)**
- `PUT /api/students/{id}` — patch email/name/company, only provided fields updated
- ETL UpsertAsync now checks `overridden_fields ? 'field_name'` before overwriting
- Override flags accumulate in JSONB — never erased by ETL
- Portal: pencil icon on each attendee row opens EditAttendeeModal
- "Send Follow-Up" button on course detail header, shows warning if no template

### Needs testing
- Deploy V37 + V38 migrations (`flyway migrate` or `deploy_db.sh`)
- Set `Smtp:Host`, `Smtp:User`, `Smtp:Pass` in production `appsettings.json` (same creds as ETL)
- Test send on a completed course — confirm email arrives, CC working
- Verify ETL does not overwrite an overridden email field after next sync cycle

### Technical debt logged
- Item 5 (MCP tools: update_student, send_post_course_email) deferred — straightforward to add
- Template UI editor (settings page) not yet built — templates editable via `PUT` API for now
- No send history/log yet — if retransmission needed, no record of what was sent

## Operational Tips
- Check bagile.co.uk/c-wiki for operational docs (Coda — needs login, not accessible from Claude)
- Course schedule spreadsheet: Google Sheet 1WLMLfkqeFfIr-G8XYhvwMBcGTrmjWm-yejJB794TC00
- Bookings dashboard: Google Sheet 1H8Z0Ts2gDSyCxeEuyPH0SJDieo_KdnVDUzKtrVADQLo
- Both sheets are publicly accessible via gviz endpoint
