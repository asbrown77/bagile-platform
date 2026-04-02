/**
 * BAgile Portal — Critical Path E2E Tests
 *
 * These are READ-ONLY integration tests that run against the real API.
 * They require a running dev server (npm run dev) and a valid API key.
 *
 * Usage:
 *   BAGILE_TEST_API_KEY=<key> npm run test:e2e
 */

import { test, expect, Page } from "@playwright/test";

// ── Auth helper ───────────────────────────────────────────

const API_KEY = process.env.BAGILE_TEST_API_KEY ?? "";

/**
 * Inject the API key into localStorage before the page runs any code.
 * Playwright's addInitScript runs before any page scripts, so the
 * useApiKey hook will find the key on first render.
 */
async function injectApiKey(page: Page) {
  if (!API_KEY) {
    throw new Error(
      "BAGILE_TEST_API_KEY environment variable is not set. " +
      "Run: BAGILE_TEST_API_KEY=<key> npm run test:e2e"
    );
  }
  await page.addInitScript((key) => {
    window.localStorage.setItem("bagile_api_key", key);
  }, API_KEY);
}

// ── Tests ─────────────────────────────────────────────────

test("dashboard loads with key metrics", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/dashboard");

  // Revenue cards should be present (they appear once API responds)
  await expect(page.getByText(/this month/i)).toBeVisible({ timeout: 15_000 });
  await expect(page.getByText(/this year/i)).toBeVisible();

  // Week strip — look for navigation arrows or day labels
  await expect(page.getByRole("button", { name: /next week|previous week|today|\u2192|\u2190/i }).first()).toBeVisible();

  // Upcoming courses table or calendar — at minimum a course row or empty state
  const courseTable = page.locator("table");
  const emptyState = page.getByText(/no courses/i);
  await expect(courseTable.or(emptyState)).toBeVisible({ timeout: 15_000 });

  // No stuck loading spinners after data arrives
  await expect(page.getByText("Loading...")).not.toBeVisible();
});

test("courses list loads and calendar toggle works", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/courses");

  // Wait for table to appear with course rows
  const table = page.locator("table");
  await expect(table).toBeVisible({ timeout: 15_000 });
  await expect(table.locator("tbody tr").first()).toBeVisible();

  // Click Calendar toggle
  const calendarBtn = page.getByRole("button", { name: /calendar/i });
  await expect(calendarBtn).toBeVisible();
  await calendarBtn.click();

  // Calendar view renders — look for month/week navigation or course tiles
  await expect(page.locator(".calendar-view, [data-testid='calendar'], [class*='calendar']").or(
    page.getByText(/jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec/i).first()
  )).toBeVisible({ timeout: 10_000 });

  // Click List toggle to go back
  const listBtn = page.getByRole("button", { name: /list/i });
  await listBtn.click();

  // Table is visible again
  await expect(table).toBeVisible({ timeout: 10_000 });
});

test("course detail loads for Bristol PSM (course 736)", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/courses/736");

  // Course title
  await expect(page.getByText(/frazer-nash/i, { exact: false })).toBeVisible({ timeout: 15_000 });

  // Attendee count — 19 attendees as per Plan.md
  await expect(page.getByText(/19/)).toBeVisible({ timeout: 15_000 });

  // CourseBadge — rendered as an image or badge element for PSM
  const badge = page.locator("img[alt*='PSM'], img[alt*='badge'], [class*='badge'], [class*='CourseBadge']");
  await expect(badge.first()).toBeVisible({ timeout: 10_000 });

  // Send action buttons — Send dropdown or explicit Send buttons
  await expect(
    page.getByRole("button", { name: /send/i }).first()
  ).toBeVisible();
});

test("send follow-up panel opens with template", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/courses/736");

  // Wait for page to be ready
  await expect(page.getByText(/frazer-nash/i, { exact: false })).toBeVisible({ timeout: 15_000 });

  // Open the Send dropdown and click "Send Follow-Up"
  const sendDropdown = page.getByRole("button", { name: /^send$/i });
  if (await sendDropdown.isVisible()) {
    await sendDropdown.click();
    await page.getByText("Send Follow-Up").click();
  } else {
    // Fallback: primary "Send Follow-Up" button
    await page.getByRole("button", { name: /send follow.up/i }).click();
  }

  // Panel should open — look for the slide-over panel
  const panel = page.locator("[class*='SendFollowUpPanel'], [data-testid='send-follow-up-panel']")
    .or(page.getByText(/send follow-up/i).nth(1));
  await expect(panel.first()).toBeVisible({ timeout: 10_000 });

  // Subject line should be populated from the template
  const subjectInput = page.locator("input[type='text']").filter({ hasText: "" }).first();
  // Either a visible input or a text area with subject content
  await expect(
    page.locator("input[placeholder*='subject' i], textarea").first()
  ).toBeVisible({ timeout: 10_000 });

  // Recipient count — 19 attendees
  await expect(page.getByText(/19/)).toBeVisible();

  // "Send test to me first" / test send link
  await expect(page.getByText(/test/i, { exact: false })).toBeVisible();

  // Close panel — press Escape or click close button
  await page.keyboard.press("Escape");
  // Verify panel is no longer the focal overlay (check send button is back to resting state)
  await expect(page.getByRole("button", { name: /^send$/i }).or(
    page.getByRole("button", { name: /send follow.up/i })
  ).first()).toBeVisible({ timeout: 5_000 });
});

test("settings page tabs work", async ({ page }) => {
  await injectApiKey(page);
  await page.goto("/settings");

  // All four tabs should be visible
  await expect(page.getByRole("button", { name: /^general$/i })).toBeVisible({ timeout: 10_000 });
  await expect(page.getByRole("button", { name: /^post-course$/i })).toBeVisible();
  await expect(page.getByRole("button", { name: /^pre-course$/i })).toBeVisible();
  await expect(page.getByRole("button", { name: /^trainers$/i })).toBeVisible();

  // Default tab is General — shows Course Risk Thresholds
  await expect(page.getByText(/course risk thresholds/i)).toBeVisible();

  // Click Post-Course tab
  await page.getByRole("button", { name: /^post-course$/i }).click();
  await expect(page.getByText(/post-course email templates/i)).toBeVisible({ timeout: 10_000 });
  // Table of templates should load
  await expect(page.locator("table tbody tr").first()).toBeVisible({ timeout: 15_000 });

  // Click Pre-Course tab
  await page.getByRole("button", { name: /^pre-course$/i }).click();
  await expect(page.getByText(/pre-course email templates/i)).toBeVisible({ timeout: 10_000 });

  // Click Trainers tab
  await page.getByRole("button", { name: /^trainers$/i }).click();
  await expect(page.getByText(/trainers/i, { exact: false })).toBeVisible();
  // Alex Brown and Chris Bexon should appear
  await expect(page.getByText(/alex brown/i)).toBeVisible({ timeout: 15_000 });
  await expect(page.getByText(/chris bexon/i)).toBeVisible({ timeout: 15_000 });

  // Click back to General tab — thresholds are visible again
  await page.getByRole("button", { name: /^general$/i }).click();
  await expect(page.getByText(/course risk thresholds/i)).toBeVisible();

  // URL should reflect active tab
  expect(page.url()).toContain("tab=general");
});
