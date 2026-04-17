import { chromium } from 'playwright';

export interface ScrumOrgHealthResult {
  success: boolean;
  loginPageReachable: boolean;
  courseManagementReachable: boolean;
  errorMessage?: string;
  durationMs: number;
}

export async function checkScrumOrgHealth(
  username: string,
  password: string
): Promise<ScrumOrgHealthResult> {
  const start = Date.now();
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  let loginPageReachable = false;
  let courseManagementReachable = false;

  try {
    // Check login page is reachable
    const loginResponse = await page.goto('https://www.scrum.org/user/login', {
      waitUntil: 'networkidle',
    });
    loginPageReachable = loginResponse?.ok() ?? false;

    // Attempt authentication
    // TODO: verify selectors against live scrum.org
    await page.fill('input[name="name"]', username);
    await page.fill('input[name="pass"]', password);
    await page.click('input[type="submit"]');
    await page.waitForURL((url) => !url.toString().includes('/user/login'), { timeout: 15_000 });

    // Check course management is reachable after login
    const manageResponse = await page.goto('https://www.scrum.org/admin/courses/manage', {
      waitUntil: 'networkidle',
    });
    courseManagementReachable = manageResponse?.ok() ?? false;

    return {
      success: true,
      loginPageReachable,
      courseManagementReachable,
      durationMs: Date.now() - start,
    };
  } catch (err) {
    return {
      success: false,
      loginPageReachable,
      courseManagementReachable,
      errorMessage: (err as Error).message,
      durationMs: Date.now() - start,
    };
  } finally {
    await browser.close();
  }
}
