/**
 * Visual check of production calendar — no API key required.
 * Runs as an unauthenticated user to see what the calendar page shows.
 *
 * Run: PLAYWRIGHT_BASE_URL=https://portal.bagile.co.uk npx playwright test e2e/calendar-visual.spec.ts --reporter=line
 */

import { test, expect } from "@playwright/test";

test("calendar page — check what renders without auth", async ({ page }) => {
  await page.goto("/calendar");
  await page.waitForLoadState("networkidle");

  // Screenshot to see what the page looks like
  await page.screenshot({ path: "test-results/calendar-noauth.png", fullPage: true });

  const title = await page.title();
  const bodyText = await page.locator("body").innerText();
  console.log("Page title:", title);
  console.log("Page text (first 500 chars):", bodyText.substring(0, 500));
});

test("calendar page — check what renders after login redirect", async ({ page }) => {
  // Visit login page first
  await page.goto("/login");
  await page.waitForLoadState("networkidle");
  await page.screenshot({ path: "test-results/login-page.png", fullPage: true });

  const loginText = await page.locator("body").innerText();
  console.log("Login page text:", loginText.substring(0, 300));
});

test("API health and calendar endpoint respond", async ({ page }) => {
  // Check the API is up
  const apiResponse = await page.request.get("https://api.bagile.co.uk/health");
  console.log("API health:", apiResponse.status(), await apiResponse.text());
  expect(apiResponse.status()).toBe(200);
});
