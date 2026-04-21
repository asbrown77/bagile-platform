import { Router } from 'express';
import { Pool } from 'pg';
import { chromium as chromiumExtra } from 'playwright-extra';
import StealthPlugin from 'puppeteer-extra-plugin-stealth';
import { CreateScrumOrgCourseUseCase } from '../../../application/use-cases/create-scrumorg-course/CreateScrumOrgCourseUseCase.js';
import { PlaywrightRunner } from '../../../infrastructure/adapters/playwright/PlaywrightRunner.js';
import { buildCredentialResolver, getCredentialStore } from '../../../infrastructure/credentials/buildCredentialResolver.js';

chromiumExtra.use(StealthPlugin());

const useCase = new CreateScrumOrgCourseUseCase(new PlaywrightRunner());

export function createPlaywrightRouter(pool: Pool): Router {
  const router = Router();

  /**
   * POST /playwright/create-scrumorg-course
   *
   * Called by bagile-api to create a scrum.org course listing via Playwright.
   * Body: { courseType, trainerName, startDate, endDate, registrationUrl, trainerId? }
   * Returns: { success, courseUrl?, errorMessage?, durationMs }
   */
  router.post('/create-scrumorg-course', async (req, res) => {
    const { courseType, trainerName, startDate, endDate, registrationUrl, trainerId } = req.body ?? {};

    if (!courseType || !trainerName || !startDate || !endDate || !registrationUrl) {
      res.status(400).json({
        error: 'Missing required fields: courseType, trainerName, startDate, endDate, registrationUrl',
      });
      return;
    }

    const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
    const userId = (typeof trainerId === 'string' && trainerId.trim())
      ? trainerId.trim()
      : (process.env['PA_USER_ID'] ?? 'alex');
    const credentialResolver = buildCredentialResolver(userId, tenantId);

    const result = await useCase.execute({
      courseType,
      trainerName,
      startDate,
      endDate,
      registrationUrl,
      tenantId,
      credentialResolver,
    });

    if (result.success) {
      res.json(result);
    } else {
      res.status(500).json(result);
    }
  });

  /**
   * POST /playwright/scrumorg-login
   *
   * Logs in to Scrum.org via Playwright for the given trainerId, stores the session
   * cookies back into pa_user_credentials, and returns the result.
   * Body: { trainerId: string }
   * Returns: { success, cookiesJson?, errorMessage?, durationMs }
   */
  router.post('/scrumorg-login', async (req, res) => {
    const { trainerId } = req.body ?? {};
    if (typeof trainerId !== 'string' || !trainerId.trim()) {
      res.status(400).json({ error: 'trainerId is required' });
      return;
    }

    const userId = trainerId.trim();
    const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
    const credentialResolver = buildCredentialResolver(userId, tenantId);

    const username = await credentialResolver('scrumorg_username');
    const password = await credentialResolver('scrumorg_password');

    if (!username || !password) {
      res.status(400).json({ error: `No scrumorg_username/password found for userId=${userId}` });
      return;
    }

    const start = Date.now();
    const executablePath = process.env['PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH'] || undefined;
    const launchArgs = [
      '--disable-blink-features=AutomationControlled',
      ...(executablePath ? ['--no-sandbox', '--disable-setuid-sandbox'] : []),
    ];

    let browser: Awaited<ReturnType<typeof chromiumExtra.launch>> | undefined;
    try {
      browser = await chromiumExtra.launch({ headless: true, executablePath, args: launchArgs });
      const context = await browser.newContext({
        userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36',
        viewport: { width: 1280, height: 800 },
        locale: 'en-GB',
      });
      const page = await context.newPage();

      // Scrum.org migrated to FusionAuth OAuth: /user/login now redirects to
      // accounts.scrum.org. Fields are #loginId / #password, not name/pass.
      await page.goto('https://www.scrum.org/user/login', { waitUntil: 'networkidle' });
      await page.waitForSelector('#loginId', { timeout: 30_000 });
      await page.fill('#loginId', username);
      await page.fill('#password', password);
      await page.locator('#password').press('Enter');
      // Wait for the OAuth callback to complete and land back on www.scrum.org
      await page.waitForURL(
        (url) => url.toString().includes('www.scrum.org') && !url.toString().includes('accounts.scrum.org'),
        { timeout: 30_000 },
      );

      const cookies = await context.cookies();
      const cookiesJson = JSON.stringify(cookies);
      const durationMs = Date.now() - start;

      const store = getCredentialStore();
      if (store) {
        await store.set(userId, tenantId, 'scrumorg_session_cookies', cookiesJson);
      }

      res.json({ success: true, cookiesJson, durationMs });
    } catch (err) {
      const durationMs = Date.now() - start;
      res.status(500).json({ success: false, errorMessage: (err as Error).message, durationMs });
    } finally {
      await browser?.close();
    }
  });

  /**
   * GET /playwright/debug
   *
   * Diagnostic endpoint — checks whether the DB is reachable from this container
   * and whether scrumorg_session_cookies exists in bagile.service_config.
   * Protected by apiKeyAuth (any valid key; no admin requirement).
   */
  router.get('/debug', async (_req, res) => {
    let dbConnected = false;
    let cookieFound = false;
    let cookieLength = 0;
    let error: string | null = null;

    try {
      const { rows } = await pool.query<{ value: string }>(
        `SELECT value FROM bagile.service_config WHERE key = $1`,
        ['scrumorg_session_cookies'],
      );
      dbConnected = true;
      if (rows[0]?.value) {
        cookieFound = true;
        cookieLength = rows[0].value.length;
      }
    } catch (err) {
      error = (err as Error).message;
    }

    res.json({ cookieFound, cookieLength, dbConnected, error });
  });

  return router;
}
