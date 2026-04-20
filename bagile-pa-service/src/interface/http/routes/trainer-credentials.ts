import { Router } from 'express';
import { Pool } from 'pg';
import { PostgresCredentialStore } from '../../../infrastructure/credentials/PostgresCredentialStore.js';
import { requireAdmin } from '../middleware/apiKeyAuth.js';

const USER_ID_RE = /^[\w-]{1,100}$/;
const KEY_RE = /^[a-z0-9_]{1,100}$/;

export function createTrainerCredentialsRouter(pool: Pool): Router {
  const router = Router();
  const store = new PostgresCredentialStore(pool);

  router.use(requireAdmin);

  /**
   * GET /trainer-credentials/:userId
   *
   * Lists credential keys present for the given userId and returns the
   * scrumorg_username in plaintext (so the UI can pre-fill it).
   * Returns: { keys: string[], username: string | null }
   */
  router.get('/:userId', async (req, res) => {
    const { userId } = req.params;
    if (!USER_ID_RE.test(userId)) {
      res.status(400).json({ error: 'Invalid userId format' });
      return;
    }

    const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
    const keys = await store.listKeys(userId, tenantId);
    const username = keys.includes('scrumorg_username')
      ? (await store.get(userId, tenantId, 'scrumorg_username')) ?? null
      : null;

    res.json({ keys, username });
  });

  /**
   * PUT /trainer-credentials/:userId/:key
   *
   * Upserts a credential value for the given userId.
   * Body: { value: string }
   */
  router.put('/:userId/:key', async (req, res) => {
    const { userId, key } = req.params;
    if (!USER_ID_RE.test(userId)) {
      res.status(400).json({ error: 'Invalid userId format' });
      return;
    }
    if (!KEY_RE.test(key)) {
      res.status(400).json({ error: 'Invalid key format — use lowercase letters, digits, and underscores only' });
      return;
    }

    const { value } = req.body as { value?: unknown };
    if (typeof value !== 'string' || value.length === 0) {
      res.status(400).json({ error: 'Body must include a non-empty "value" string' });
      return;
    }

    const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
    await store.set(userId, tenantId, key, value);
    res.json({ ok: true, userId, key });
  });

  /**
   * DELETE /trainer-credentials/:userId/:key
   *
   * Removes a credential for the given userId.
   */
  router.delete('/:userId/:key', async (req, res) => {
    const { userId, key } = req.params;
    if (!USER_ID_RE.test(userId)) {
      res.status(400).json({ error: 'Invalid userId format' });
      return;
    }

    const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
    const deleted = await store.delete(userId, tenantId, key);
    if (!deleted) {
      res.status(404).json({ error: `Credential "${key}" not found for userId="${userId}"` });
      return;
    }
    res.json({ ok: true, userId, key });
  });

  return router;
}
