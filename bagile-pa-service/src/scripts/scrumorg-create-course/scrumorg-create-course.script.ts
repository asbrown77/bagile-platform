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
    const courseUrl = await saveCourse(page);

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

  // TODO: verify selectors against live scrum.org
  await page.fill('input[name="name"]', username);
  await page.fill('input[name="pass"]', password);
  await page.click('input[type="submit"]');

  await page.waitForURL((url) => !url.toString().includes('/user/login'), { timeout: 15_000 });
}

async function navigateToCourseManagement(page: Page): Promise<void> {
  // TODO: verify URL and access permissions on live scrum.org
  await page.goto('https://www.scrum.org/admin/courses/manage', { waitUntil: 'networkidle' });
}

async function findAndCopyLatestCourse(
  page: Page,
  courseType: string,
  trainerName: string
): Promise<void> {
  // TODO: verify row/cell selectors against live scrum.org course management table
  // Assumption: table rows contain course title and trainer name as text, with a Copy link per row
  const rows = page.locator('table tbody tr');
  const count = await rows.count();

  for (let i = 0; i < count; i++) {
    const row = rows.nth(i);
    const rowText = await row.innerText();

    if (rowText.includes(courseType) && rowText.includes(trainerName)) {
      // TODO: verify the "Copy" link/button selector on live scrum.org
      const copyLink = row.locator('a:has-text("Copy"), button:has-text("Copy")').first();
      await copyLink.click();
      await page.waitForLoadState('networkidle');
      return;
    }
  }

  throw new Error(`No course found for type "${courseType}" and trainer "${trainerName}"`);
}

async function editCopiedCourse(
  page: Page,
  startDate: string,
  endDate: string,
  registrationUrl: string
): Promise<void> {
  // TODO: verify field selectors against live scrum.org course edit form
  await clearAndFill(page, 'input[name="start_date"]', startDate);
  await clearAndFill(page, 'input[name="end_date"]', endDate);
  await clearAndFill(page, 'input[name="field_registration_url[und][0][url]"]', registrationUrl);
}

async function clearAndFill(page: Page, selector: string, value: string): Promise<void> {
  const field = page.locator(selector).first();
  await field.clear();
  await field.fill(value);
}

async function saveCourse(page: Page): Promise<string> {
  // TODO: verify submit button selector against live scrum.org
  await page.click('input[type="submit"][value="Save"]');
  await page.waitForLoadState('networkidle');
  return page.url();
}
