# BAgile PA Service — Architecture Plan

## Context

BAgile currently runs all PA automation locally via Claude Code. Every morning check,
Trello update, transfer, and draft label requires Claude to make multiple raw API calls
within the conversation — burning tokens, repeating the same work, and keeping everything
locked to one person's machine.

The goal is a **PA Service** that:
- Wraps repeatable tasks as MCP tools Claude calls instead of raw API calls
- Exposes the same tasks in the Portal UI so Alex or Chris can trigger them manually
- Supports multiple users (Alex as admin, Chris as trainer) from day one
- Detects and reports when automations break (especially Playwright ones)
- Runs test-first, with clean architecture principles throughout

---

## 1. Service Boundary

**Decision: New `bagile-pa-service/` folder inside this monorepo.**

Start here for zero deployment friction. Shares TypeScript tooling, CI patterns, and
the existing `api-client.ts` conventions. If it ever needs to serve another business,
the internal folder structure already mirrors what an extracted repo would look like.

---

## 2. Dual Interface Principle

This is the core architectural constraint. The same use cases serve two consumers:

```
[Claude Code]  → MCP tools   → Use Cases → Infrastructure adapters
[Portal UI]    → REST API    → Use Cases → Infrastructure adapters
[n8n webhooks]              → Use Cases → Infrastructure adapters
```

Use cases are interface-agnostic. A `TransferFooEventTicketUseCase` doesn't know
whether Alex clicked a button or Claude called `pa_transfer_fooevent_ticket`.

---

## 3. Portal Task Inbox

PA tasks (morning brief items, pending transfers, flagged actions) are stored as
`PaTask` entities. Claude populates the inbox; Alex and Chris work through it in the
Portal. This means:

- Alex doesn't need Claude Code open to see what needs doing
- Tasks can be marked complete from the Portal
- Claude can check task status before recommending actions
- Chris gets his own filtered view (trainer-relevant tasks only)

---

## 4. Folder Structure

```
bagile-pa-service/
├── package.json
├── tsconfig.json
├── vitest.config.ts
├── .env.example
│
├── src/
│   ├── domain/                          # No external dependencies
│   │   ├── entities/
│   │   │   ├── Tenant.ts
│   │   │   ├── Integration.ts           # tenantId, type, credentials, status
│   │   │   ├── Automation.ts            # id, tenantId, name, type, lastRun, lastStatus
│   │   │   ├── HealthRecord.ts          # automationId, runAt, status, durationMs, error
│   │   │   └── PaTask.ts                # id, tenantId, userId, type, payload, status, createdAt
│   │   ├── ports/                       # Interfaces — implemented in infrastructure
│   │   │   ├── IGmailPort.ts
│   │   │   ├── ICalendarPort.ts
│   │   │   ├── ITrelloPort.ts
│   │   │   ├── IWooCommercePort.ts
│   │   │   ├── IXeroPort.ts
│   │   │   ├── IMailchimpPort.ts
│   │   │   ├── IBagileApiPort.ts
│   │   │   ├── IPlaywrightRunnerPort.ts
│   │   │   ├── IAlertPort.ts
│   │   │   ├── IHealthRepository.ts
│   │   │   └── IPaTaskRepository.ts
│   │   └── value-objects/
│   │       ├── AutomationResult.ts      # { status: 'pass'|'fail'|'skip', message, data }
│   │       └── TenantContext.ts         # { tenantId, userId, timezone }
│   │
│   ├── application/                     # Use Cases — orchestrates domain + ports
│   │   ├── use-cases/
│   │   │   ├── morning-brief/
│   │   │   │   ├── MorningBriefUseCase.ts
│   │   │   │   └── MorningBriefUseCase.test.ts
│   │   │   ├── label-gmail-draft/
│   │   │   │   ├── LabelGmailDraftUseCase.ts
│   │   │   │   └── LabelGmailDraftUseCase.test.ts
│   │   │   ├── update-trello-card/
│   │   │   │   ├── UpdateTrelloCardUseCase.ts
│   │   │   │   └── UpdateTrelloCardUseCase.test.ts
│   │   │   ├── check-pending-transfers/
│   │   │   │   ├── CheckPendingTransfersUseCase.ts
│   │   │   │   └── CheckPendingTransfersUseCase.test.ts
│   │   │   ├── transfer-fooevent-ticket/
│   │   │   │   ├── TransferFooEventTicketUseCase.ts
│   │   │   │   └── TransferFooEventTicketUseCase.test.ts
│   │   │   ├── cancel-course/
│   │   │   │   ├── CancelCourseUseCase.ts
│   │   │   │   └── CancelCourseUseCase.test.ts
│   │   │   ├── lookup-xero-invoice/
│   │   │   │   ├── LookupXeroInvoiceUseCase.ts
│   │   │   │   └── LookupXeroInvoiceUseCase.test.ts
│   │   │   └── create-scrumorg-course/
│   │   │       ├── CreateScrumOrgCourseUseCase.ts
│   │   │       └── CreateScrumOrgCourseUseCase.test.ts
│   │   ├── services/
│   │   │   ├── HealthService.ts         # Records run results, threshold alerting
│   │   │   ├── PaTaskService.ts         # Create, list, complete tasks in the inbox
│   │   │   └── TenantResolver.ts        # API key → TenantContext
│   │   └── dtos/
│   │       ├── MorningBriefResult.ts
│   │       └── TrelloCardUpdate.ts
│   │
│   ├── infrastructure/                  # Implements ports — external systems
│   │   ├── adapters/
│   │   │   ├── gmail/GmailAdapter.ts
│   │   │   ├── calendar/GoogleCalendarAdapter.ts
│   │   │   ├── trello/TrelloAdapter.ts
│   │   │   ├── woocommerce/WooCommerceAdapter.ts
│   │   │   ├── xero/XeroAdapter.ts
│   │   │   ├── mailchimp/MailchimpAdapter.ts
│   │   │   ├── bagile-api/BagileApiAdapter.ts
│   │   │   └── playwright/PlaywrightRunner.ts
│   │   ├── persistence/
│   │   │   ├── JsonHealthRepository.ts  # File-based, no DB overhead
│   │   │   ├── JsonPaTaskRepository.ts  # File-based initially
│   │   │   └── TenantConfigLoader.ts    # Reads tenants.json / env vars
│   │   └── alerts/
│   │       └── GmailAlertAdapter.ts     # Sends email on automation failure
│   │
│   ├── interface/
│   │   ├── mcp/                         # Claude interface
│   │   │   ├── server.ts                # Composition root, stdio transport
│   │   │   ├── tools/
│   │   │   │   ├── morning-brief.tool.ts
│   │   │   │   ├── label-gmail-draft.tool.ts
│   │   │   │   ├── update-trello-card.tool.ts
│   │   │   │   ├── check-pending-transfers.tool.ts
│   │   │   │   ├── transfer-fooevent-ticket.tool.ts
│   │   │   │   ├── cancel-course.tool.ts
│   │   │   │   ├── lookup-xero-invoice.tool.ts
│   │   │   │   ├── create-scrumorg-course.tool.ts
│   │   │   │   ├── health-status.tool.ts
│   │   │   │   └── pa-tasks.tool.ts     # list/complete inbox tasks
│   │   │   └── middleware/tenant-auth.ts
│   │   └── http/                        # Portal + n8n interface
│   │       ├── server.ts
│   │       └── routes/
│   │           ├── automations.ts       # POST /automations/:name/run
│   │           ├── tasks.ts             # GET/PATCH /tasks (PA inbox)
│   │           ├── health.ts            # GET /health
│   │           └── webhooks.ts
│   │
│   └── scripts/                         # Playwright automation scripts (NOT test specs)
│       ├── fooevent-transfer/
│       │   ├── fooevent-transfer.script.ts
│       │   ├── fooevent-transfer.health.ts   # Smoke: can we reach wp-admin?
│       │   └── README.md                     # Steps, selectors, last-known-good date
│       └── scrumorg-create-course/
│           ├── scrumorg-create-course.script.ts
│           ├── scrumorg-create-course.health.ts
│           └── README.md
│
└── tests/
    ├── integration/
    │   ├── mcp-tools.test.ts
    │   └── health-service.test.ts
    └── fixtures/
        ├── tenant-alex.ts
        └── mock-adapters.ts
```

---

## 5. Multi-Tenancy Model

`TenantContext` is threaded through every use case — no ambient globals:

```typescript
// domain/value-objects/TenantContext.ts
export interface TenantContext {
  tenantId: string;   // 'bagile' | future
  userId: string;     // 'alex' | 'chris'
  timezone: string;   // 'Europe/London'
}
```

Tenant configuration lives in `tenants.json` (gitignored, loaded from env in CI):

```json
{
  "tenants": [{
    "id": "bagile",
    "name": "BAgile Ltd",
    "timezone": "Europe/London",
    "users": [
      { "id": "alex", "role": "admin", "email": "alex@bagile.co.uk", "apiKey": "..." },
      { "id": "chris", "role": "trainer", "email": "chris@bagile.co.uk", "apiKey": "..." }
    ],
    "integrations": {
      "gmail": { "enabled": true, "credentialsEnvVar": "BAGILE_GMAIL_CREDS" },
      "trello": { "enabled": true, "boardId": "hNs49hi4", "credentialsEnvVar": "BAGILE_TRELLO_KEY" },
      "xero": { "enabled": true, "tenantId": "aef46d85-...", "credentialsEnvVar": "BAGILE_XERO_CREDS" },
      "woocommerce": { "enabled": true, "baseUrl": "https://bagile.co.uk", "credentialsEnvVar": "BAGILE_WOO_CREDS" },
      "bagile-api": { "enabled": true, "baseUrl": "https://api.bagile.co.uk", "credentialsEnvVar": "BAGILE_API_KEY" },
      "playwright": { "enabled": true, "wpAdminUrl": "https://bagile.co.uk/wp-admin", "credentialsEnvVar": "BAGILE_WP_CREDS" }
    }
  }]
}
```

Adding a new tenant = one JSON block + env vars. No code changes.

**Per-user role gating:** `Automation` entity has `allowedRoles`. Use cases check
`TenantContext.userId` role before executing. Chris sees trainer-relevant tasks only.

---

## 6. Playwright Automation Pattern

### Script vs Test vs Health Check

| Type | Purpose | Runs when |
|------|---------|----------|
| `*.script.ts` | Performs real work (transfer ticket, create course) | Called by use case via `IPlaywrightRunnerPort` |
| `*.health.ts` | Verifies target is reachable — no side effects | Scheduled + before running main script |
| `e2e/*.spec.ts` | Tests Portal UI | CI only |

### Script structure

```typescript
export interface FooEventTransferInput {
  orderId: number;
  oldCourseScheduleId: number;
  newCourseScheduleId: number;
  wpAdminUrl: string;
  wpUsername: string;
  wpPassword: string;
}

export interface FooEventTransferOutput {
  success: boolean;
  ticketId?: string;
  errorMessage?: string;
  screenshotPath?: string;   // captured on any step failure
}

export async function runFooEventTransfer(
  page: Page,
  input: FooEventTransferInput
): Promise<FooEventTransferOutput>
```

Each step uses explicit waits (`waitForSelector`) not arbitrary `sleep`.
On failure: screenshot → structured error → return `{ success: false }`.
Scripts are versioned in Git. Each `README.md` records: steps, selectors relied on,
last-verified date, past breakages.

### `IPlaywrightRunnerPort`

```typescript
export interface IPlaywrightRunnerPort {
  run(options: {
    scriptName: string;
    tenantId: string;
    input: Record<string, unknown>;
    headless?: boolean;
  }): Promise<{
    success: boolean;
    output?: Record<string, unknown>;
    errorMessage?: string;
    screenshotPath?: string;
    durationMs: number;
  }>;
}
```

---

## 7. Health Monitoring

```typescript
// domain/entities/HealthRecord.ts
export interface HealthRecord {
  id: string;
  automationId: string;
  tenantId: string;
  runAt: Date;
  status: 'pass' | 'fail' | 'degraded';
  durationMs: number;
  errorMessage?: string;
  screenshotPath?: string;
  triggeredBy: 'schedule' | 'manual' | 'webhook';
}
```

**Two check levels per automation:**
1. **Connectivity** (every 4h) — can we reach the service? Fast, cheap.
2. **Smoke run** (daily) — does the Playwright script complete end-to-end with a safe test input?

**`HealthService`** records every run result. After 3 consecutive failures, calls
`IAlertPort.sendAlert()`. First implementation: `GmailAlertAdapter` emails
alex@bagile.co.uk with the error, screenshot link, and a manual re-run link.

**`pa_health_status` MCP tool** — Claude can request health table at any time:

```
pa_health_status({ tenantId: "bagile" })
→ automation | last run | status | last error
```

---

## 8. TDD Approach

**Domain layer — pure unit tests, no mocks needed**
Entities and value objects have no dependencies. Test behaviour directly.

**Application layer — unit tests with mocked ports**
Use cases depend only on port interfaces. Inject test doubles:

```typescript
// MorningBriefUseCase.test.ts
const mockGmail: IGmailPort = {
  getUnreadSummary: vi.fn().mockResolvedValue({ count: 3, subjects: [...] }),
};

it("marks brief as degraded when one integration fails", async () => {
  mockGmail.getUnreadSummary.mockRejectedValueOnce(new Error("Gmail timeout"));
  const result = await useCase.execute(alexContext, { date: "2026-04-16" });
  expect(result.status).toBe("degraded");
  expect(result.warnings).toContain("Gmail unavailable");
});
```

Test files co-located with the file under test (same folder, `.test.ts` suffix).

**Infrastructure layer — integration tests with recorded responses**
Follow the `vi.stubGlobal("fetch", ...)` pattern from `bagile-mcp-server/src/__tests__/api-client.test.ts`.

**MCP interface layer — tool registration tests**
Verify tools registered, Zod validation rejects bad inputs, tenant resolution fires before use case.

**Playwright health scripts — real browser, no side effects**
Run nightly via `npm run test:health`. Check login and plugin presence only.

---

## 9. Build Order — Maximum Value First

### Phase 1 — Skeleton ✅ DONE
Working MCP server Claude can connect to. Intentionally minimal — no tenancy yet.
- `src/tools/ping.ts` + `ping.test.ts` — `handlePing()` reads `PA_USER_ID` env var
- `src/server.ts` — `createServer()` registers `pa_ping` tool
- `src/index.ts` — entry point, stdio transport
- Registered in `.mcp.json` as `bagile-pa` with `PA_USER_ID=alex`

**TenantResolver deferred to Phase 2** — no use case to design against yet.
MCP stdio identity = env var (`PA_USER_ID`). API key auth is for HTTP interface only.

**Written first:** `ping.test.ts` — both tests pass ✅

### Phase 2 — Morning Brief (1 session)
Claude calls `pa_morning_brief` instead of 4+ raw API calls.
- `IGmailPort`, `ICalendarPort`, `ITrelloPort`, `IBagileApiPort`
- `MorningBriefUseCase` + test
- All four adapters
- `morning-brief.tool.ts`

**Write first:** `MorningBriefUseCase.test.ts` (happy path + Gmail degradation)

### Phase 3 — PA Task Inbox (1 session)
Morning brief results saved as tasks; Portal can list and complete them.
- `PaTask` entity, `IPaTaskRepository`, `JsonPaTaskRepository`
- `PaTaskService`
- `pa-tasks.tool.ts` (MCP)
- `GET /tasks`, `PATCH /tasks/:id` (HTTP — for Portal)

**Write first:** `PaTaskService.test.ts`

### Phase 4 — Health Monitoring (1 session)
Every automation reports pass/fail; Claude and Portal can see health status.
- `HealthRecord`, `IHealthRepository`, `IAlertPort`
- `HealthService` + test
- `JsonHealthRepository`
- `GmailAlertAdapter`
- Wrap use case execution in health recording
- `health-status.tool.ts`

**Write first:** `HealthService.test.ts` (3-failure threshold, alert fires once)

### Phase 5 — FooEvents Playwright Script (1-2 sessions)
First Playwright automation. Proves the runner pattern.
- `IPlaywrightRunnerPort`
- `TransferFooEventTicketUseCase` + test (mocked runner)
- `PlaywrightRunner` adapter
- `fooevent-transfer.script.ts`
- `fooevent-transfer.health.ts`
- `transfer-fooevent-ticket.tool.ts` (MCP)
- Portal button (uses HTTP `POST /automations/fooevent-transfer/run`)

**Write first:** `TransferFooEventTicketUseCase.test.ts`, then `fooevent-transfer.health.ts`

### Phase 6 — Remaining API Automations (1 session)
- `pa_check_pending_transfers`
- `pa_update_trello_card`
- `pa_cancel_course`
- `pa_lookup_xero_invoice`
- `pa_label_gmail_draft`

### Phase 7 — Scrum.org Playwright Script
Most complex. Build after FooEvents runner pattern is proven.

---

## Key Architectural Decisions

**Dependency direction:** `domain/` and `application/` never import from `infrastructure/`.
Ports are interfaces in `domain/ports/`; adapters implement them in `infrastructure/`.

**No shared adapter state between tenants:** Every adapter instantiated per-request with
tenant-scoped credentials. No singleton adapters holding credentials.

**Playwright fails loudly:** Screenshot on any step failure. Never silently succeed.
Structured error return, recorded to `HealthService`.

**Use existing PostgreSQL for persistence (Phase 3+):** The platform already has
PostgreSQL with Flyway migrations. `HealthRecord` and `PaTask` tables go there —
not JSON files as originally planned. Flyway migrations in `/migrations/` following
existing `VN__name.sql` convention. No separate DB, no JSON file hacks.

**MCP tool naming:** `pa_` prefix throughout. No collision with existing `bagile-mcp-server` tools.

**Use cases are small:** One folder, one file, under 100 lines. If a use case grows,
it's doing too much — split it.

---

## Reference Files (existing patterns to follow)

- `bagile-mcp-server/src/index.ts` — MCP server setup, tool registration, stdio transport
- `bagile-mcp-server/src/api-client.ts` — fetch wrapper pattern (X-Api-Key, typed responses)
- `bagile-mcp-server/src/__tests__/api-client.test.ts` — Vitest pattern (`vi.stubGlobal("fetch")`)
- `bagile-portal/e2e/critical-path.spec.ts` — existing Playwright structure
- `CLAUDE.md` — non-negotiable standards that apply equally to this service
