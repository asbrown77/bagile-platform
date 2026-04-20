import type { Page } from 'playwright';

export interface ScrumOrgCreateCourseInput {
  scrumorgUsername: string;
  scrumorgPassword: string;
  // Optional pre-authenticated session cookies (JSON array of Playwright cookie objects).
  // When present, the script injects them directly, bypassing the login form entirely.
  // This avoids Cloudflare bot-detection on the login page from data-center IPs.
  scrumorgSessionCookies?: string;
  courseType: string;
  trainerName: string;
  startDate: string;       // YYYY-MM-DD
  endDate: string;         // YYYY-MM-DD
  registrationUrl: string;
}

export interface ScrumOrgCreateCourseOutput {
  success: boolean;
  courseUrl?: string;
  errorMessage?: string;
  screenshotPath?: string;
}

// Maps portal course type codes → full names used in scrum.org course management table
const COURSE_TYPE_NAMES: Record<string, string> = {
  APSSD:  'Applying Professional Scrum for Software Development',
  PSM:    'Professional Scrum Master',
  PSMO:   'Professional Scrum Master',
  PSPO:   'Professional Scrum Product Owner',
  PSK:    'Professional Scrum with Kanban',
  PALE:   'Professional Agile Leadership - Essentials',
  EBM:    'Professional Agile Leadership - Evidence Based Management',
  PSPOA:  'Professional Scrum Product Owner - Advanced',
  PSMA:   'Professional Scrum Master - Advanced',
  PSFS:   'Professional Scrum Facilitation Skills',
  APS:    'Applying Professional Scrum',
  PSU:    'Professional Scrum with User Experience',
  // AI Essentials courses — added Apr 2026.
  // Scrum.org admin table title uses a dash: "Professional Scrum Master - AI Essentials"
  PSMAI:  'Professional Scrum Master - AI Essentials',
  PSPOAI: 'Professional Scrum Product Owner - AI Essentials',
};

export async function runScrumorgCreateCourse(
  page: Page,
  input: ScrumOrgCreateCourseInput
): Promise<ScrumOrgCreateCourseOutput> {
  const screenshotPath = `screenshots/scrumorg-${Date.now()}.png`;

  try {
    await loginToScrumOrg(
      page,
      input.scrumorgUsername,
      input.scrumorgPassword,
      input.scrumorgSessionCookies,
    );
    await navigateToCourseManagement(page);
    await findAndCopyLatestCourse(page, input.courseType, input.trainerName);
    await editCopiedCourse(page, input.startDate, input.endDate, input.registrationUrl);
    const courseUrl = await saveAndSchedule(page);

    return { success: true, courseUrl };
  } catch (err) {
    await page.screenshot({ path: screenshotPath }).catch(() => undefined);
    return {
      success: false,
      errorMessage: (err as Error).message,
      screenshotPath,
    };
  }
}

async function loginToScrumOrg(
  page: Page,
  username: string,
  password: string,
  sessionCookiesJson?: string,
): Promise<void> {
  // --- Cookie-based auth (preferred): bypass login form entirely ---
  // Session cookies can be stored in company settings and injected here,
  // avoiding any Cloudflare challenge on the login page from data-center IPs.
  if (sessionCookiesJson) {
    let cookies: Array<{
      name: string; value: string; domain: string; path: string;
      expires?: number; httpOnly?: boolean; secure?: boolean;
      sameSite?: 'Strict' | 'Lax' | 'None';
    }>;
    try {
      cookies = JSON.parse(sessionCookiesJson);
    } catch {
      throw new Error('scrumorg_session_cookies is not valid JSON');
    }

    await page.context().addCookies(cookies);
    // Proceed directly — navigateToCourseManagement will detect if the session
    // is invalid (redirect to /user/login or /access-denied) and throw there.
    // Navigating to a separate verification page is unnecessary and adds another
    // Cloudflare-challenged roundtrip on data-center IPs.
    return;
  }

  // --- Form-based auth (fallback) ---
  // Use networkidle so any Cloudflare JS challenge completes before we inspect the page.
  await page.goto('https://www.scrum.org/user/login', { waitUntil: 'networkidle' });

  // Reliably check if already authenticated: look for the Drupal logout link.
  // Do NOT rely solely on URL — bot-detection redirects can send headless Chrome away from
  // /user/login even when not logged in, causing a false "already authenticated" signal.
  const isLoggedIn = await page.locator('a[href*="/user/logout"]').first()
    .isVisible()
    .catch(() => false);

  if (isLoggedIn) return;

  // Navigate explicitly to login page if we were redirected elsewhere
  if (!page.url().includes('/user/login')) {
    await page.goto('https://www.scrum.org/user/login', { waitUntil: 'networkidle' });
  }

  // Wait for the login form to be ready (guards against slow JS rendering).
  // Cloudflare on data-center IPs often blocks this page — if the selector never
  // appears, surface a clear message rather than a generic timeout.
  const formStart = Date.now();
  await page.waitForSelector('input[name="name"]', { timeout: 15_000 }).catch((err) => {
    const durationS = Math.round((Date.now() - formStart) / 1000);
    throw new Error(
      `Cloudflare is blocking the login page (input[name='name'] never appeared). ` +
      `Session cookies were absent. Duration so far: ~${durationS}s. Original: ${(err as Error).message}`,
    );
  });

  await page.locator('input[name="name"]').fill(username);
  await page.locator('input[name="pass"]').fill(password);
  await page.locator('input[type="submit"]').click();
  await page.waitForURL((url) => !url.toString().includes('/user/login'), { timeout: 20_000 });

  // Verify we are actually authenticated after the redirect
  await page.waitForSelector('a[href*="/user/logout"]', { timeout: 10_000 });
}

async function navigateToCourseManagement(page: Page): Promise<void> {
  await page.goto('https://www.scrum.org/admin/courses/manage', { waitUntil: 'networkidle' });

  // Confirm we landed on the manage page, not a redirect to login
  if (page.url().includes('/user/login') || page.url().includes('/access-denied')) {
    await page.screenshot({ path: `screenshots/scrumorg-manage-denied-${Date.now()}.png` }).catch(() => undefined);
    throw new Error(`Cannot access course management page — not authenticated (redirected to ${page.url()})`);
  }

  // Wait for the table to appear
  await page.waitForSelector('table tbody tr', { timeout: 15_000 });
}

async function findAndCopyLatestCourse(
  page: Page,
  courseType: string,
  trainerName: string
): Promise<void> {
  const fullCourseName = COURSE_TYPE_NAMES[courseType.toUpperCase()] ?? courseType;

  // Two-pass search:
  //   Pass 1: prefer the trainer's own most-recent course (best template for dates/settings).
  //   Pass 2: fall back to any trainer's course of that type — used the first time a trainer
  //           runs a new course type and has no personal history to copy from.
  const passes: Array<{ label: string; match: (rowText: string) => boolean }> = [
    {
      label: `${fullCourseName} + ${trainerName}`,
      match: (t) => t.includes(fullCourseName) && t.includes(trainerName),
    },
    {
      label: `${fullCourseName} (any trainer)`,
      match: (t) => t.includes(fullCourseName),
    },
  ];

  for (const pass of passes) {
    // Navigate back to page 1 of the admin table for each pass
    await page.goto('https://www.scrum.org/admin/courses/manage', { waitUntil: 'networkidle' });
    await page.waitForSelector('table tbody tr', { timeout: 15_000 });

    let pageNum = 1;
    while (true) {
      const rows = page.locator('table tbody tr');
      const count = await rows.count();

      for (let i = 0; i < count; i++) {
        const row = rows.nth(i);
        const rowText = await row.innerText();

        if (pass.match(rowText)) {
          const replicateHref = await row.locator('a[href*="replicate"]').getAttribute('href').catch(() => null);
          if (!replicateHref) throw new Error(`No replicate link for row: ${rowText.substring(0, 100)}`);

          await page.goto(`https://www.scrum.org${replicateHref}`, { waitUntil: 'networkidle' });

          const confirmBtn = page.locator('input[type="submit"][value="Copy"], button:has-text("Copy")').first();
          await confirmBtn.waitFor({ state: 'visible', timeout: 10_000 });
          await confirmBtn.click();
          await page.waitForLoadState('networkidle');
          return;
        }
      }

      const nextLink = page.locator('a[title="Go to next page"], a.pager__link--next, li.pager__item--next a').first();
      const nextVisible = await nextLink.isVisible().catch(() => false);
      if (!nextVisible) break;

      pageNum++;
      await nextLink.click();
      await page.waitForLoadState('networkidle');

      if (pageNum > 20) break; // safety limit
    }
    // Pass 1 exhausted — try pass 2
  }

  throw new Error(`No course found for type "${courseType}" (${fullCourseName}) — tried trainer-specific and any-trainer search`);
}

async function editCopiedCourse(
  page: Page,
  startDate: string,
  endDate: string,
  registrationUrl: string
): Promise<void> {
  // After copy confirmation, scrum.org redirects to the new course VIEW page.
  // Extract node ID from URL (format: /courses/...-{nodeId}) and navigate to edit.
  const nodeId = page.url().match(/-(\d+)$/)?.[1];
  if (!nodeId) throw new Error(`Could not extract node ID from URL: ${page.url()}`);

  await page.goto(`https://www.scrum.org/node/${nodeId}/edit`, { waitUntil: 'networkidle' });

  // Update dates — IDs confirmed against live scrum.org edit form
  await page.locator('#edit-field-start-date-0-value-date').fill(startDate);
  await page.locator('#edit-field-end-date-0-value-date').fill(endDate);

  // Expand "Registration & Price" section if not already open
  const urlField = page.getByRole('textbox', { name: 'External Registration Site URL' });
  const isVisible = await urlField.isVisible();
  if (!isVisible) {
    await page.getByRole('button', { name: 'Registration & Price' }).click();
    await urlField.waitFor({ state: 'visible', timeout: 5_000 });
  }
  await urlField.fill(registrationUrl);
}

async function saveAndSchedule(page: Page): Promise<string> {
  // "Save and Schedule" saves the course and shows the schedule confirmation page
  await page.getByRole('button', { name: 'Save and Schedule' }).click();
  await page.waitForLoadState('networkidle');

  // Schedule Class Confirmation page — click to go live
  await page.getByRole('button', { name: 'Schedule Class' }).click();
  await page.waitForLoadState('networkidle');

  return page.url();
}
