# Sprint 28 — Unified Course Hub

**Goal:** Make `/calendar` the single place for all course management. Plan, draft (Planned state), publish, import, export — public and private — without a second page.

**We'll know it worked when:** Alex opens `/calendar`, creates a PSM planned course, edits its date, publishes to WooCommerce in one click, adds a private course from the same screen, and imports a batch of planned courses from a CSV — without ever visiting `/courses`.

---

## Context

### What exists today
- `/calendar` — FullCalendar month/week + list view, shows ALL courses (planned + WooCommerce-synced), Add Course button (public Planned only), side panel with gateway checklist + Go Live, trainer/status filters
- `/courses` — separate list page showing WooCommerce courses ONLY (no planned), separate "Create Private Course" button
- `/courses/[id]` — course detail: attendees, emails, transfers — **keep this exactly as-is**
- `POST /api/planned-courses` — creates a single planned course
- `PATCH /api/planned-courses/{id}` — updates a planned course
- `DELETE /api/planned-courses/{id}` — deletes a planned course
- Private courses created via `POST /api/courses/private` (separate endpoint from planned courses)

### Status model (do not change terminology)
- **Planned** — portal only, grey. Not in WooCommerce, not on Scrum.org.
- **Partial Live** — amber. Some gateways published.
- **Live** — green. All applicable gateways published.
- **Cancelled** — red. Terminal.
- Private courses skip Planned — they are created directly as live.

### Key files
- `bagile-portal/app/(authenticated)/calendar/page.tsx` (1111 lines) — main calendar page
- `bagile-portal/app/(authenticated)/courses/page.tsx` — list page to be retired
- `bagile-portal/components/courses/CreatePrivateCoursePanel.tsx` — existing private course creation panel
- `Bagile/Bagile.Api/Controllers/PlannedCoursesController.cs` — planned course API
- `lib/calendarHelpers.ts` — course type options, gateway config, status colours
- `lib/api.ts` — API client functions

---

## Items

| # | Item | Size |
|---|------|------|
| 1 | Add Course modal: Public/Private toggle — Private calls existing private course endpoint, skips Planned state | S |
| 2 | Edit planned course from side panel — Edit button, reuse modal pre-filled, PATCH on submit | S |
| 3 | Trainer filter: load from DB (GET /api/trainers) instead of hardcoded ["all","AB","CB"] | XS |
| 4 | List view: add search + date range filters (Upcoming / year / All) | S |
| 5 | Retire /courses list → redirect to /calendar?view=list, preserve query params. /courses/[id] untouched. | M |
| 6 | `POST /api/planned-courses/bulk` — accept array of planned courses, validate per-row, return per-row result | S |
| 7 | CSV import UI: upload .csv → parse → preview table → confirm → bulk create | M |
| 8 | CSV export: button downloads all visible calendar courses as CSV | S |
| 9 | MCP tool: `create_planned_course` (single + array, calls bulk endpoint) | S |

**CSV format:** `courseType,startDate,endDate,trainer,isVirtual,venue,notes`
Example: `PSM,2026-06-02,2026-06-03,Alex Brown,true,,`

---

## Rules
- `/courses/[id]` detail pages are NOT touched
- "Planned" is the correct term — not "draft" anywhere in UI copy
- Private courses skip Planned — straight to live
- Trainer filter must be dynamic — no hardcoded initials
- Bulk endpoint: partial imports allowed, return `{index, success, id?, error?}` per row
- File size ≤ 300 lines, function size ≤ 30 lines — split if needed
- No SQL interpolation, parameterized queries only

---

## Suggested build order
1. Items 3, 2, 1 (quick wins — all in calendar/page.tsx)
2. Items 4, 5 (list view + redirect)
3. Items 6, 7, 8 (import/export — backend first, then UI)
4. Item 9 (MCP tool — last, depends on bulk endpoint)
