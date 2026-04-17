import type { Page } from 'playwright';

export interface FooEventTransferInput {
  wpAdminUrl: string;
  wpUsername: string;
  wpPassword: string;
  oldTicketPostId: number;
  newProductId: number;
  attendeeFirstName: string;
  attendeeLastName: string;
  attendeeEmail: string;
  attendeeCompany: string;
  designation: string;
  headless?: boolean;
}

export interface FooEventTransferOutput {
  success: boolean;
  newTicketPostId?: number;
  errorMessage?: string;
  screenshotPath?: string;
}

export async function runFooEventTransfer(
  page: Page,
  input: FooEventTransferInput
): Promise<FooEventTransferOutput> {
  const { wpAdminUrl, wpUsername, wpPassword, oldTicketPostId, newProductId } = input;

  try {
    await loginToWpAdmin(page, wpAdminUrl, wpUsername, wpPassword);
    await cancelOldTicket(page, wpAdminUrl, oldTicketPostId, input.designation);
    await createNewTicket(page, wpAdminUrl, newProductId, input);
    await resendTicket(page);
    const newTicketPostId = await getNewTicketPostId(page);

    return { success: true, newTicketPostId };
  } catch (err) {
    const screenshotPath = await captureFailureScreenshot(page);
    return {
      success: false,
      errorMessage: (err as Error).message,
      screenshotPath,
    };
  }
}

async function loginToWpAdmin(
  page: Page,
  wpAdminUrl: string,
  username: string,
  password: string
): Promise<void> {
  await page.goto(`${wpAdminUrl}/wp-login.php`);
  await page.fill('#user_login', username);
  await page.fill('#user_pass', password);
  await page.click('#wp-submit');
  await page.waitForURL(/wp-admin/);
}

async function cancelOldTicket(
  page: Page,
  wpAdminUrl: string,
  ticketPostId: number,
  designation: string
): Promise<void> {
  await page.goto(`${wpAdminUrl}/post.php?post=${ticketPostId}&action=edit`);
  await page.fill('#WooCommerceEventsAttendeeName2', designation);
  const statusSelects = page.locator('select[name*="WooCommerceEventsTicketStatus"]');
  const count = await statusSelects.count();
  for (let i = 0; i < count; i++) {
    await statusSelects.nth(i).selectOption('Canceled');
  }
  await page.evaluate(() => document.getElementById('publish')?.click());
  await page.waitForURL(/updated=1/);
}

async function createNewTicket(
  page: Page,
  wpAdminUrl: string,
  productId: number,
  attendee: FooEventTransferInput
): Promise<void> {
  await page.goto(`${wpAdminUrl}/post-new.php?post_type=event_magic_tickets`);
  await page.waitForSelector('#WooCommerceEventsEvent');

  await page.evaluate(
    (id) => { (window as any).jQuery('#WooCommerceEventsEvent').val(String(id)).trigger('change'); },
    productId
  );
  await page.evaluate(() => {
    (window as any).jQuery('#WooCommerceEventsClientID').val('3').trigger('change');
  });

  await page.fill('#WooCommerceEventsAttendeeName', attendee.attendeeFirstName);
  await page.fill('#WooCommerceEventsAttendeeName1', attendee.attendeeLastName);
  await page.fill('#WooCommerceEventsAttendeeEmail', attendee.attendeeEmail);
  if (attendee.attendeeCompany) {
    await page.fill('#WooCommerceEventsAttendeeName3', attendee.attendeeCompany);
  }
  await page.fill('#WooCommerceEventsAttendeeName2', attendee.designation);

  await page.evaluate(() => document.getElementById('publish')?.click());
  await page.waitForURL(/post=\d+.*action=edit/);
}

async function resendTicket(page: Page): Promise<void> {
  await page.click('#WooCommerceEventsResendTicket');
  await page.waitForSelector('.notice-success, .updated', { timeout: 10_000 });
}

async function getNewTicketPostId(page: Page): Promise<number> {
  const url = page.url();
  const match = url.match(/post=(\d+)/);
  if (!match) throw new Error(`Cannot parse ticket post ID from URL: ${url}`);
  return parseInt(match[1], 10);
}

async function captureFailureScreenshot(page: Page): Promise<string | undefined> {
  try {
    const screenshotPath = `screenshots/fooevent-transfer-${Date.now()}.png`;
    await page.screenshot({ path: screenshotPath });
    return screenshotPath;
  } catch {
    return undefined;
  }
}
