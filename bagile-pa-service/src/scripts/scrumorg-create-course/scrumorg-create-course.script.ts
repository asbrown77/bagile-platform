import type { Page } from 'playwright';

export interface ScrumOrgCreateCourseInput {
  scrumorgUsername: string;
  scrumorgPassword: string;
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
  APSSD: 'Applying Professional Scrum for Software Development',
  PSM:   'Professional Scrum Master',
  PSMO:  'Professional Scrum Master',
  PSPO:  'Professional Scrum Product Owner',
  PSK:   'Professional Scrum with Kanban',
  PALE:  'Professional Agile Leadership - Essentials',
  EBM:   'Professional Agile Leadership - Evidence Based Management',
  PSPOA: 'Professional Scrum Product Owner - Advanced',
  PSMA:  'Professional Scrum Master - Advanced',
  PSFS:  'Professional Scrum Facilitation Skills',
  APS:   'Applying Professional Scrum',
  PSU:   'Professional Scrum with User Experience',
};

export async function runScrumorgCreateCourse(
  page: Page,
  input: ScrumOrgCreateCourseInput
): Promise<ScrumOrgCreateCourseOutput> {
  const screenshotPath = `screenshots/scrumorg-${Date.now()}.png`;

  try {
    await loginToScrumOrg(page, input.scrumorgUsername, input.scrumorgPassword);
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

async function loginToScrumOrg(page: Page, username: string, password: string): Promise<void> {
  await page.goto('https://www.scrum.org/user/login', { waitUntil: 'networkidle' });

  // If already logged in, scrum.org redirects away from /user/login immediately
  if (!page.url().includes('/user/login')) return;

  await page.locator('input[name="name"]').fill(username);
  await page.locator('input[name="pass"]').fill(password);
  await page.locator('input[type="submit"]').click();
  await page.waitForURL((url) => !url.toString().includes('/user/login'), { timeout: 15_000 });
}

async function navigateToCourseManagement(page: Page): Promise<void> {
  await page.goto('https://www.scrum.org/admin/courses/manage', { waitUntil: 'networkidle' });
}

async function findAndCopyLatestCourse(
  page: Page,
  courseType: string,
  trainerName: string
): Promise<void> {
  const fullCourseName = COURSE_TYPE_NAMES[courseType.toUpperCase()] ?? courseType;

  // The admin table may be paginated — keep clicking "next" until we find a match or exhaust all pages
  let pageNum = 1;
  while (true) {
    const rows = page.locator('table tbody tr');
    const count = await rows.count();

    for (let i = 0; i < count; i++) {
      const row = rows.nth(i);
      const rowText = await row.innerText();

      if (rowText.includes(fullCourseName) && rowText.includes(trainerName)) {
        // Open the dropdown — the toggle is the second item in the operations dropbutton
        const toggleBtn = row.locator('.dropbutton__toggle').first();
        await toggleBtn.click();

        // Copy link appears in the dropdown after toggle
        const copyLink = row.locator('a[href*="replicate"]').first();
        await copyLink.waitFor({ state: 'visible', timeout: 5_000 });
        await copyLink.click();
        await page.waitForLoadState('networkidle');

        // Confirmation page: "Are you sure you want to replicate Course...?"
        await page.getByRole('button', { name: 'Copy' }).click();
        await page.waitForLoadState('networkidle');
        return;
      }
    }

    // Try to go to the next page
    const nextLink = page.locator('a[title="Go to next page"], a.pager__link--next, li.pager__item--next a').first();
    const nextVisible = await nextLink.isVisible().catch(() => false);
    if (!nextVisible) break;

    pageNum++;
    await nextLink.click();
    await page.waitForLoadState('networkidle');

    if (pageNum > 20) break; // safety limit
  }

  throw new Error(`No course found for type "${courseType}" (${fullCourseName}) and trainer "${trainerName}"`);
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
