# BAgile Platform — Progress & Lessons Learned

## Known Bugs — Pick Up Next Sprint (logged 17 Apr 2026)

### ~~BUG-1: Multi-ticket sync — Maxwell Gravelle missing~~ FIXED etl-v1.12.3
- Replaced Math.Min truncation in WooOrderParser with full-iteration logic. Should self-correct on next ETL cycle.
- **Verify:** Check Maxwell Gravelle appears on NobleProg PSM course after next ETL run.

### ~~BUG-2: Multi-ticket sync — PSPO-AI 31 Mar missing attendees~~ FIXED etl-v1.12.3
- Same fix as BUG-1. Verify order #12902 shows all 5 attendees after next ETL run.

### ~~BUG-3: cancel_course endpoint returns 500~~ FIXED api-v4.5.4
- Controller now has try/catch returning Problem() with exception message. Handler null-checks before UpdateStatusAsync.
- **Note:** Root cause for schedule ID 90 specifically not yet confirmed — fix ensures error is now surfaced rather than silent 500.

### ~~BUG-4: GET /planned-courses endpoint + portal UI missing~~ FIXED api-v4.5.4 + portal-v6.5.22
- GET /api/planned-courses live, ordered by start_date with trainer name via LEFT JOIN.
- Portal: Planned Courses page added to sidebar under Operate.

### INFRA-1: GHCR PAT expired on Hetzner server
- **Symptom:** Docker pulls from `ghcr.io` fail — PAT has expired.
- **Fix:** Alex generates new GitHub PAT with `read:packages` scope, then:
  ```
  ssh root@142.132.227.7
  echo 'NEW_PAT_HERE' | docker login ghcr.io -u asbrown77 --password-stdin
  ```

---

## Session 20 Apr 2026 — Course Schedule Polish + Bug Fixes

**Status:** Complete — deployed portal-v1.3.0, api-v4.5.25

### What was built

**Decision due filter pill**
- Promoted "Decision due" from a static legend dot to a clickable toggle pill
- When active: filters to courses where `decisionDeadline` is set and is today or in the future
- Matches style of existing status pills (red-outlined when active)
- Added `decisionFilter` state + filter logic in `applyFilters`

**List view: Public/Private column**
- Removed padlock icon from course code cell in list view (still kept on calendar blocks)
- Added dedicated "Type" column after course code: shows "Private" (amber) or "Public" (grey)
- Hidden on mobile (`hidden sm:table-cell`)

**Cancel button broken for live courses (Bug Fix)**
- Root cause: `handleCancel` extracted numeric ID and always called `PATCH /api/planned-courses/{id}` — but live courses have `schedule-*` IDs from `course_schedules`, not `planned_courses`
- Fix: branch on ID prefix — `schedule-*` → `POST /api/course-schedules/{id}/cancel`, `planned-*` → `PATCH /api/planned-courses/{id}`
- Second root cause: `UpdatePlannedCourseCommand` had no `Status` field, so planned-course cancels were silently ignored — added `Status?` to command and handler

**Gateway status doesn't refresh after publish (Bug Fix)**
- Root cause 1: stale closure — `handlePublish` manually searched `events` immediately after `await loadEvents()`, but React hadn't applied the `setEvents(data)` update yet
- Root cause 2: `listEvents` never reloaded after publish — only `loadEvents` (calendar) was called
- Root cause 3: ecommerce publish changes event ID (`planned-123` → `schedule-*`), so sync-by-ID fails
- Fix: extracted `loadListEvents` as a reusable callback; `handlePublish` and `handleCancel` both call `Promise.all([loadEvents(), loadListEvents()])`; sync `useEffect` now searches `[...events, ...listEvents]` and falls back to courseType+startDate match if ID not found

### Lessons learned

**Portal deploy requires a `portal-v*` tag** — the CI workflow only runs the SSH deploy step on `portal-v*` tags or `workflow_dispatch`. Pushes to `main` build the Docker image but never restart the server. Always push a `portal-v*` tag after portal changes, not just `main`.

---

## Sprint 23 — Professional Private Courses (1 Apr 2026)

**Status:** Complete — committed in 4 separate commits.

**What was built:**
- V45 migration: `acronym` column on organisations table, seeded for known orgs
- `GET /api/organisations/search?q=` — type-ahead endpoint, hits organisations table directly (not derived query), returns id/name/acronym/ptnTier, top 10
- `POST /api/organisations` — create new org from portal; creates alias entry with name
- `UpdatePrivateCourseCommand` now accepts `ClientOrganisationId`; persisted in `UpdatePrivateCourseAsync`
- `CourseScheduleDetailDto` + query: exposes `clientOrganisationId`, `clientOrganisationName`, `clientOrganisationAcronym` via LEFT JOIN
- `OrganisationTypeAhead` component — debounced search, dropdown with name+acronym+PTN tier badge, "Create as new" inline form with auto-suggested acronym, chip on selection, graceful failure fallback to free text
- `privateCourseHelpers.ts` — `generateCourseName()` and `generateInvoiceRef()` shared utilities
- `CreatePrivateCoursePanel` — full rewrite: org type-ahead, auto-generated course name and invoice ref (editable, resetable), JSON template updated to accept `organisationName`/`organisationAcronym`
- `EditPrivateCoursePanel` — pre-populates org chip from course data, reset-to-auto buttons for name/ref
- Course detail page: prefers `clientOrganisationName` over parsed title; renders org as clickable link

**Needs testing:**
- Create private course → select existing org → verify name/ref auto-generate, submit stores `client_organisation_id`
- Create private course → "Create as new org" flow → verify org created in DB then selected
- Edit private course (legacy, no org set) → search and select org → save stores ID
- Edit private course (with org) → chip pre-populated on open
- Reset to auto buttons regenerate correctly after org change
- Course detail page shows client org as link (not just parsed text)
- V45 migration runs cleanly (acronym column added, existing orgs seeded)

**Known debt logged:**
- The `suggestAcronym` function takes the first letter of each word — good for 3+ word names, produces single letters for single-word orgs (e.g. "DVSA" → "D"). Trainer should always review before saving.

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
- ~~Item 5 (MCP tools: update_student, send_post_course_email) deferred~~ **RESOLVED Sprint 18**
- ~~Template UI editor (settings page) not yet built~~ **RESOLVED Sprint 18**
- No send history/log yet — if retransmission needed, no record of what was sent

## Sprint 19 (1 Apr 2026) — Private Course Polish

### What was built (3 commits)

**Commit 1 — API: PUT /course-schedules/{id} + DELETE /course-schedules/{id}/attendees/{enrolmentId}**
- `UpdatePrivateCourseFields` value object in domain layer
- `UpdatePrivateCourseAsync` in repository (SQL WHERE guards `is_public = false`)
- `UpdatePrivateCourseCommand` + handler (CQRS, re-fetches and returns refreshed DTO)
- `CancelPrivateEnrolmentAsync` with SQL JOIN guard: validates enrolment belongs to the course AND course is private — returns false (→ 404) if not
- `RemovePrivateAttendeeCommand` + handler using the safe cancel method
- `PUT {id}` and `DELETE {id}/attendees/{enrolmentId}` wired in `CourseSchedulesController`

**Commit 2 — Frontend: EditPrivateCoursePanel + API client functions**
- `UpdatePrivateCourseRequest` type and `updatePrivateCourse()` in `lib/api.ts`
- `removePrivateAttendee()` helper in `lib/api.ts`
- `EditPrivateCoursePanel` slide-over: seeds from current course, virtual/in-person conditional sections, calls PUT on save

**Commit 3 — Frontend: Course detail page polish (items 2-6)**
- Remove Attendee: Trash2 icon, confirm dialog, `handleRemoveAttendee` with loading state
- Transfer/Refund hidden on private courses; Edit + Remove shown
- Table simplified for private (3 cols: Name, Email, Actions); public unchanged (6 cols)
- Client org parsed from title suffix after " - " (Building2 icon in info bar)
- Over-capacity: `{active}/{capacity} ⚠️` in red when active > capacity
- Edit Course button in header (secondary, pencil icon, private only)

### Needs testing
- PUT /api/course-schedules/{id} — only updates is_public=false rows (test on a public course id: should be a no-op / 404)
- DELETE /api/course-schedules/{id}/attendees/{enrolmentId} — confirm enrolment moves to "cancelled" in DB, reappears in History tab
- Verify: Transfer and Refund buttons absent on private course attendee rows
- Verify: Organisation/Country columns absent on private course table
- Verify: Client name appears in header for "PSM - Acme Corp" format titles
- Verify: Over-capacity warning shows red when attendee count exceeds capacity
- Deploy: no DB migration needed (no schema changes)

### Technical debt
- Client org is parsed from title — fragile for free-form titles. Long-term: use `client_organisation_id` FK + org name lookup. Tracked in P2.5 item 2.
- PUT endpoint does not validate `StartDate <= EndDate` — relies on frontend form validation only.

## Sprint 18 (1 Apr 2026) — Post-Course Emails End-to-End

### What was built (4 commits)

**Commit 1 — Monitoring API: CourseStatus field**
- Added `CourseStatus` to `CourseMonitoringDto` — exposes raw WP status (publish, cancelled, sold_out, draft)
- Handler maps `course.Status` → `CourseStatus` alongside computed `MonitoringStatus`
- Portal can now distinguish cancelled courses from healthy ones on dashboard

**Commit 2 — V39 migration: seed all remaining post-course templates**
- Full templates (resources, books, assessment details) for: PSM, PSPO-A, PSU, PAL-EBM
- Placeholder templates (marked "needs customising"): PSM-AI, PSPO-AI, PSK, PAL-E, PSM-A, PSFS, APS-SD
- All inserts use `ON CONFLICT (course_type) DO NOTHING` — safe to re-run
- Template for PSM includes: 8 stances, Tech Debt Simulator, Retromat, Planning Poker, Liberating Structures
- PSPO-A includes 9 recommended books: Impact Mapping, Lean UX, BVSSH, Good to Great, Extreme Ownership, etc.
- PAL-EBM includes: EBM Guide, OKRs, Turn the Ship Around video, What Matters

**Commit 3 — Portal: Post-Course Templates editor in Settings**
- New `PostCourseTemplatesEditor` component in `settings/page.tsx`
- Lists all templates; "needs customising" amber badge on placeholders
- Click Edit → form with subject (+ variable hint) and HTML textarea (24 rows)
- Save calls `PUT /api/templates/post-course/{courseType}`, shows last updated timestamp
- Reads API key from localStorage (same pattern as rest of portal)

**Commit 4 — MCP: update_student + send_post_course_email tools**
- `update_student` — PUT /api/students/{id}, all fields optional, override note, ETL-safe
- `send_post_course_email` — POST /api/templates/post-course/send/{id}, supports delayNote and courseTypeOverride
- Added `apiPut()` helper to api-client (mirrors apiPost but uses PUT method)
- TypeScript build passes clean

### Item 5: Favicon — already resolved
- `bagile-portal/public/favicon.png` exists (9096 bytes, copied in Sprint 15)
- `layout.tsx` already references `/favicon.png` via Next.js metadata icons API
- No code changes needed

### Item 1: SMTP — already wired, ops action needed
- `SmtpEmailService` registered in DI via `AddInfrastructureServices` since Sprint 16
- `appsettings.json` has correct `Smtp:Host/Port/User/Pass/From` keys
- **Ops action required:** Populate SMTP env vars on production API server (same values as ETL service)

### Needs testing
- Deploy V39 migration (`flyway migrate` or `deploy_db.sh`)
- Populate SMTP env vars on API server, test send on a completed course
- Verify template editor in Settings loads and saves correctly
- Test MCP tools: `update_student` (check ETL doesn't overwrite on next sync), `send_post_course_email`
- Verify `courseStatus` field appears in monitoring API response

## Operational Tips
- Check bagile.co.uk/c-wiki for operational docs (Coda — needs login, not accessible from Claude)
- Course schedule spreadsheet: Google Sheet 1WLMLfkqeFfIr-G8XYhvwMBcGTrmjWm-yejJB794TC00
- Bookings dashboard: Google Sheet 1H8Z0Ts2gDSyCxeEuyPH0SJDieo_KdnVDUzKtrVADQLo
- Both sheets are publicly accessible via gviz endpoint
