# BAgile Platform — Claude Code Standards

## Project Overview
BAgile Platform is the golden source for BAgile Ltd's training business. It aggregates data from WooCommerce, Xero, FooEvents, and Stripe into a unified API for orders, students, enrolments, course schedules, transfers, and organisations.

**Stack:** .NET 8.0, PostgreSQL 16, Dapper, MediatR (CQRS), Clean Architecture
**Production:** https://api.bagile.co.uk
**Repo:** github.com/asbrown77/bagile-platform

## Architecture
```
Bagile.Domain          → Entities, Repository interfaces
Bagile.Application     → CQRS Queries/Commands, DTOs, Validators
Bagile.Infrastructure  → Dapper queries, API clients (Woo, Xero, FooEvents), Repositories
Bagile.Api             → Controllers, Middleware, Webhooks, Endpoints
Bagile.EtlService      → Background worker: collectors, parsers, transformers
```

## Non-Negotiable Standards

### Code Quality
- Clean Architecture: dependencies flow inward only (Domain ← Application ← Infrastructure ← Api)
- CQRS via MediatR: separate read (queries) and write (commands) paths
- Dapper for data access: no EF Core, explicit SQL, parameterized queries only
- FluentValidation at API boundary
- File size: ≤300 lines. Function size: ≤30 lines. Break up if exceeded.

### Security
- API Key authentication via X-Api-Key header (ApiKeyAuthenticationMiddleware)
- Webhook signature validation (HMAC-SHA256 for WooCommerce, Xero)
- No SQL interpolation — parameterized queries only
- No secrets in code — environment variables or appsettings (gitignored)

### Testing — Non-Negotiable
- **Always validate before declaring done.** Never claim a fix works without verifying: run `dotnet test`, then test the actual endpoint or UI path manually.
- **Verify iteratively in small steps.** Build → test → confirm → next step. Never batch multiple untested changes before checking.
- Unit tests for parsers and business logic
- Integration tests for API endpoints and ETL pipeline
- Run before every commit: `dotnet test` (API/backend), `npx tsc --noEmit` (portal TypeScript)
- Playwright E2E tests live in `bagile-portal/e2e/` — run against production with `BAGILE_TEST_API_KEY` injected
- When an endpoint changes, test it directly with curl/Playwright before marking complete

### Database
- Flyway migrations in /migrations/ (V1__name.sql format)
- Schema: `bagile` schema in PostgreSQL
- Never modify existing migrations — always create new ones
- Test migrations locally before deploying

### Git & Deployment
- Branch from main for features
- CI/CD via GitHub Actions → Hetzner
- Tag format: `api-vX.Y.Z` or `etl-vX.Y.Z`
- Never force push to main

## Key Files
- `/migrations/` — Flyway SQL migrations (V1-V25)
- `/docs/API.md` — API reference
- `/Bagile/Bagile.Api/Program.cs` — API startup, DI, middleware
- `/Bagile/Bagile.EtlService/Program.cs` — ETL worker startup
- `/Bagile/Bagile.EtlService/Services/EtlWorker.cs` — Main ETL loop
- `/Bagile/Bagile.Infrastructure/Clients/` — WooCommerce, Xero, FooEvents clients

## External Systems
- **WooCommerce:** Course products, orders, webhooks
- **Xero:** Invoices, payments, contacts (OAuth2, tenant: aef46d85-ec9c-475b-990d-5480d708605c)
- **FooEvents:** Event tickets (WooCommerce plugin)
- **Stripe:** Two accounts — Website (Bagile) and Invoices (BAgile Limited)
- **Scrum.org:** Course listings, assessments (no API, admin portal only)

## Lessons Learned
- "Sold out" on WordPress = cancelled course, not actually full — keeps scrum.org links alive
- Xero collector was disabled due to reliability issues — needs fixing before re-enabling
- ETL has a double 5-min delay bug (10 mins total) — fix when touching EtlWorker
- QA partner rate is PTN33 (33%) — auto-invoicing at RRP when coupon not applied is a known issue
- FooEvents transfers require order to be "completed" (paid) before ticket operations work
- **Dapper + nullable DB columns:** Always check if PostgreSQL columns have `NOT NULL`. If nullable, the C# row type must use nullable types (`DateTime?`, `bool?`) or Dapper will throw on NULL rows — causing a 500 that only appears when the date range includes rows with missing values.
- **Lazy LINQ after connection disposal:** `Concat().OrderBy()` returns a lazy `IEnumerable`. If the connection is disposed before enumeration, you get a runtime error. Always `.ToList()` before closing the connection or rely on in-memory sources only.
- **Portal JWT expires after 8 hours.** The `/portal/keys` endpoint returns 401 when the JWT expires. The frontend must detect this (via a typed `PortalAuthError`) and drop back to the Google Sign-In view — not silently fail with "Failed to create key".
- **Static method / instance field CS0120:** A `private static` method cannot access instance fields. Pass them as parameters explicitly.
