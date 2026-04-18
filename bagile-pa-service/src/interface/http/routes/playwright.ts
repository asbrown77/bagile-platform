import { Router } from 'express';
import { Pool } from 'pg';
import { CreateScrumOrgCourseUseCase } from '../../../application/use-cases/create-scrumorg-course/CreateScrumOrgCourseUseCase.js';
import { PlaywrightRunner } from '../../../infrastructure/adapters/playwright/PlaywrightRunner.js';
import { buildCredentialResolver } from '../../../infrastructure/credentials/buildCredentialResolver.js';

const useCase = new CreateScrumOrgCourseUseCase(new PlaywrightRunner());

export function createPlaywrightRouter(pool: Pool): Router {
  const router = Router();

  /**
   * POST /playwright/create-scrumorg-course
   *
   * Called by bagile-api to create a scrum.org course listing via Playwright.
   * Body: { courseType, trainerName, startDate, endDate, registrationUrl }
   * Returns: { success, courseUrl?, errorMessage?, durationMs }
   */
  router.post('/create-scrumorg-course', async (req, res) => {
    const { courseType, trainerName, startDate, endDate, registrationUrl } = req.body ?? {};

    if (!courseType || !trainerName || !startDate || !endDate || !registrationUrl) {
      res.status(400).json({
        error: 'Missing required fields: courseType, trainerName, startDate, endDate, registrationUrl',
      });
      return;
    }

    const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
    const userId = process.env['PA_USER_ID'] ?? 'alex';
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
