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

## Operational Tips
- Check bagile.co.uk/c-wiki for operational docs (Coda — needs login, not accessible from Claude)
- Course schedule spreadsheet: Google Sheet 1WLMLfkqeFfIr-G8XYhvwMBcGTrmjWm-yejJB794TC00
- Bookings dashboard: Google Sheet 1H8Z0Ts2gDSyCxeEuyPH0SJDieo_KdnVDUzKtrVADQLo
- Both sheets are publicly accessible via gviz endpoint
