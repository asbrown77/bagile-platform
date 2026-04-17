# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: calendar.spec.ts >> list view Upcoming shows courses
- Location: e2e\calendar.spec.ts:45:5

# Error details

```
Error: BAGILE_TEST_API_KEY not set
```

# Test source

```ts
  1   | /**
  2   |  * BAgile Portal — Calendar E2E Tests
  3   |  *
  4   |  * Tests the /calendar page: month view, list view, side panel, gateway links.
  5   |  *
  6   |  * Run against production:
  7   |  *   BAGILE_TEST_API_KEY=<key> PLAYWRIGHT_BASE_URL=https://portal.bagile.co.uk npx playwright test e2e/calendar.spec.ts
  8   |  *
  9   |  * Run against local dev server:
  10  |  *   BAGILE_TEST_API_KEY=<key> npm run test:e2e
  11  |  */
  12  | 
  13  | import { test, expect, Page } from "@playwright/test";
  14  | 
  15  | const API_KEY = process.env.BAGILE_TEST_API_KEY ?? "";
  16  | 
  17  | async function injectApiKey(page: Page) {
> 18  |   if (!API_KEY) throw new Error("BAGILE_TEST_API_KEY not set");
      |                       ^ Error: BAGILE_TEST_API_KEY not set
  19  |   await page.addInitScript((key) => {
  20  |     window.localStorage.setItem("bagile_api_key", key);
  21  |   }, API_KEY);
  22  | }
  23  | 
  24  | // ── Calendar month view ───────────────────────────────────────
  25  | 
  26  | test("calendar month view loads and shows courses", async ({ page }) => {
  27  |   await injectApiKey(page);
  28  |   await page.goto("/calendar");
  29  | 
  30  |   // FullCalendar renders
  31  |   await expect(page.locator(".fc-toolbar-title")).toBeVisible({ timeout: 15_000 });
  32  | 
  33  |   // Loading finishes
  34  |   await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });
  35  |   await expect(page.getByText("Failed to load calendar events")).not.toBeVisible();
  36  | 
  37  |   // At least one course block
  38  |   const events = page.locator(".fc-event");
  39  |   await expect(events.first()).toBeVisible({ timeout: 15_000 });
  40  |   console.log(`Calendar: ${await events.count()} course blocks`);
  41  | });
  42  | 
  43  | // ── List view — Upcoming ───────────────────────────────────────
  44  | 
  45  | test("list view Upcoming shows courses", async ({ page }) => {
  46  |   await injectApiKey(page);
  47  |   await page.goto("/calendar?view=list");
  48  | 
  49  |   // List tab is active
  50  |   await expect(page.getByRole("button", { name: /^list$/i })).toBeVisible({ timeout: 10_000 });
  51  | 
  52  |   // Loading finishes
  53  |   await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });
  54  | 
  55  |   // No error banner
  56  |   await expect(page.locator("[class*='AlertBanner'], [class*='danger']")).not.toBeVisible();
  57  | 
  58  |   // Table of courses
  59  |   const rows = page.locator("table tbody tr");
  60  |   const count = await rows.count();
  61  |   console.log(`List (Upcoming): ${count} rows`);
  62  | 
  63  |   // Should have rows OR a "no courses" state (valid if no upcoming courses)
  64  |   const noCoursesMsg = page.getByText("No courses found.");
  65  |   const hasRows = count > 0;
  66  |   const hasEmpty = await noCoursesMsg.isVisible().catch(() => false);
  67  | 
  68  |   if (!hasRows && !hasEmpty) {
  69  |     throw new Error("List view shows neither courses nor empty state — likely a load error");
  70  |   }
  71  |   if (hasRows) {
  72  |     expect(count).toBeGreaterThan(0);
  73  |   }
  74  | });
  75  | 
  76  | // ── List view — All (should always have data) ─────────────────
  77  | 
  78  | test("list view All has courses", async ({ page }) => {
  79  |   await injectApiKey(page);
  80  |   await page.goto("/calendar?view=list");
  81  | 
  82  |   await expect(page.getByRole("button", { name: /^list$/i })).toBeVisible({ timeout: 10_000 });
  83  |   await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });
  84  | 
  85  |   // Click "All" date range
  86  |   await page.getByRole("button", { name: /^all$/i }).last().click();
  87  |   await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });
  88  | 
  89  |   // Must have courses — "All" spans 2020–2030
  90  |   const rows = page.locator("table tbody tr");
  91  |   await expect(rows.first()).toBeVisible({ timeout: 15_000 });
  92  |   const count = await rows.count();
  93  |   console.log(`List (All): ${count} rows`);
  94  |   expect(count).toBeGreaterThan(0);
  95  | });
  96  | 
  97  | // ── Side panel opens ──────────────────────────────────────────
  98  | 
  99  | test("clicking a course in list opens side panel", async ({ page }) => {
  100 |   await injectApiKey(page);
  101 |   await page.goto("/calendar?view=list");
  102 | 
  103 |   await expect(page.getByRole("button", { name: /^list$/i })).toBeVisible({ timeout: 10_000 });
  104 |   await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });
  105 | 
  106 |   // Switch to All to guarantee data
  107 |   await page.getByRole("button", { name: /^all$/i }).last().click();
  108 |   await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });
  109 | 
  110 |   const firstRow = page.locator("table tbody tr").first();
  111 |   await expect(firstRow).toBeVisible({ timeout: 15_000 });
  112 |   await firstRow.click();
  113 | 
  114 |   // Gateway checklist in side panel
  115 |   await expect(page.getByText(/gateway checklist/i)).toBeVisible({ timeout: 10_000 });
  116 |   console.log("Side panel opened successfully");
  117 | });
  118 | 
```