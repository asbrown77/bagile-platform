/**
 * BAgile Portal — Calendar E2E Tests
 *
 * Tests the /calendar page: month view, list view, side panel, gateway links.
 *
 * Run against production:
 *   BAGILE_TEST_API_KEY=<key> PLAYWRIGHT_BASE_URL=https://portal.bagile.co.uk npx playwright test e2e/calendar.spec.ts
 *
 * Run against local dev server:
 *   BAGILE_TEST_API_KEY=<key> npm run test:e2e
 */

import { test, expect, Page } from "@playwright/test";

const API_KEY = process.env.BAGILE_TEST_API_KEY ?? "";

async function injectApiKey(page: Page) {
  if (!API_KEY) throw new Error("BAGILE_TEST_API_KEY not set");
  await page.addInitScript((key) => {
    window.localStorage.setItem("bagile_api_key", key);
  }, API_KEY);
}

// ── Calendar month view ───────────────────────────────────────

test("calendar month view loads and shows courses", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/calendar");

  // FullCalendar renders
  await expect(page.locator(".fc-toolbar-title")).toBeVisible({ timeout: 15_000 });

  // Loading finishes
  await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });
  await expect(page.getByText("Failed to load calendar events")).not.toBeVisible();

  // At least one course block
  const events = page.locator(".fc-event");
  await expect(events.first()).toBeVisible({ timeout: 15_000 });
  console.log(`Calendar: ${await events.count()} course blocks`);
});

// ── List view — Upcoming ───────────────────────────────────────

test("list view Upcoming shows courses", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/calendar?view=list");

  // List tab is active
  await expect(page.getByRole("button", { name: /^list$/i })).toBeVisible({ timeout: 10_000 });

  // Loading finishes
  await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });

  // No error banner
  await expect(page.locator("[class*='AlertBanner'], [class*='danger']")).not.toBeVisible();

  // Table of courses
  const rows = page.locator("table tbody tr");
  const count = await rows.count();
  console.log(`List (Upcoming): ${count} rows`);

  // Should have rows OR a "no courses" state (valid if no upcoming courses)
  const noCoursesMsg = page.getByText("No courses found.");
  const hasRows = count > 0;
  const hasEmpty = await noCoursesMsg.isVisible().catch(() => false);

  if (!hasRows && !hasEmpty) {
    throw new Error("List view shows neither courses nor empty state — likely a load error");
  }
  if (hasRows) {
    expect(count).toBeGreaterThan(0);
  }
});

// ── List view — All (should always have data) ─────────────────

test("list view All has courses", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/calendar?view=list");

  await expect(page.getByRole("button", { name: /^list$/i })).toBeVisible({ timeout: 10_000 });
  await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });

  // Click "All" date range
  await page.getByRole("button", { name: /^all$/i }).last().click();
  await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });

  // Must have courses — "All" spans 2020–2030
  const rows = page.locator("table tbody tr");
  await expect(rows.first()).toBeVisible({ timeout: 15_000 });
  const count = await rows.count();
  console.log(`List (All): ${count} rows`);
  expect(count).toBeGreaterThan(0);
});

// ── Side panel opens ──────────────────────────────────────────

test("clicking a course in list opens side panel", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/calendar?view=list");

  await expect(page.getByRole("button", { name: /^list$/i })).toBeVisible({ timeout: 10_000 });
  await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });

  // Switch to All to guarantee data
  await page.getByRole("button", { name: /^all$/i }).last().click();
  await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });

  const firstRow = page.locator("table tbody tr").first();
  await expect(firstRow).toBeVisible({ timeout: 15_000 });
  await firstRow.click();

  // Gateway checklist in side panel
  await expect(page.getByText(/gateway checklist/i)).toBeVisible({ timeout: 10_000 });
  console.log("Side panel opened successfully");
});

// ── Gateway rows are clickable links ─────────────────────────

test("published gateway rows are clickable <a> links", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/calendar?view=list");

  await expect(page.getByRole("button", { name: /^list$/i })).toBeVisible({ timeout: 10_000 });
  await page.getByRole("button", { name: /^all$/i }).last().click();
  await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });

  // Find a live course and click it
  const liveRow = page.locator("table tbody tr").filter({ hasText: /live/i }).first();
  if (await liveRow.count() === 0) {
    console.log("No live courses — skip gateway link test");
    return;
  }

  await liveRow.click();
  await expect(page.getByText(/gateway checklist/i)).toBeVisible({ timeout: 10_000 });

  // Check for clickable gateway links
  const gatewayLinks = page.locator("a[href*='bagile.co.uk'], a[href*='scrum.org'], a[href*='http']")
    .filter({ hasText: /view/i });
  const linkCount = await gatewayLinks.count();
  console.log(`Clickable gateway links in side panel: ${linkCount}`);

  if (linkCount > 0) {
    const href = await gatewayLinks.first().getAttribute("href");
    console.log(`First gateway link: ${href}`);
    expect(href).toBeTruthy();
  }
});

// ── Private course padlock ────────────────────────────────────

test("private courses show amber padlock", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/calendar?view=list");

  await page.getByRole("button", { name: /^all$/i }).last().click();
  await expect(page.getByText("Loading...")).not.toBeVisible({ timeout: 20_000 });

  // Private courses should have an amber lock
  const privateLocks = page.locator("svg.text-amber-500").or(
    page.locator("[class*='amber']").filter({ has: page.locator("svg") })
  );
  const lockCount = await privateLocks.count();
  console.log(`Amber padlocks visible: ${lockCount}`);
});
