import { chromium } from 'playwright';

export interface FooEventHealthResult {
  success: boolean;
  wpAdminReachable: boolean;
  fooEventsMenuPresent: boolean;
  errorMessage?: string;
  durationMs: number;
}

export async function checkFooEventHealth(
  wpAdminUrl: string,
  username: string,
  password: string
): Promise<FooEventHealthResult> {
  const start = Date.now();
  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage();

  try {
    await page.goto(`${wpAdminUrl}/wp-login.php`);
    await page.fill('#user_login', username);
    await page.fill('#user_pass', password);
    await page.click('#wp-submit');
    await page.waitForURL(/wp-admin/);

    const wpAdminReachable = true;
    const fooEventsMenu = await page.$('a[href*="fooevents"]');
    const fooEventsMenuPresent = fooEventsMenu !== null;

    return {
      success: fooEventsMenuPresent,
      wpAdminReachable,
      fooEventsMenuPresent,
      durationMs: Date.now() - start,
    };
  } catch (err) {
    return {
      success: false,
      wpAdminReachable: false,
      fooEventsMenuPresent: false,
      errorMessage: (err as Error).message,
      durationMs: Date.now() - start,
    };
  } finally {
    await browser.close();
  }
}
