import { Router } from 'express';
import { Pool } from 'pg';
import { PostgresCredentialStore } from '../../../infrastructure/credentials/PostgresCredentialStore.js';
import { requireAdmin } from '../middleware/apiKeyAuth.js';

const KEY_RE = /^[a-z0-9_]{1,100}$/;

export function createCredentialsRouter(pool: Pool): Router {
  const router = Router();
  const store = new PostgresCredentialStore(pool);

  // All credential routes require admin role
  router.use(requireAdmin);

  // GET /credentials — list stored key names for the caller
  router.get('/', async (req, res) => {
    const { userId, tenantId } = req.caller;
    const keys = await store.listKeys(userId, tenantId);
    res.json({ keys, userId });
  });

  // PUT /credentials/:key — upsert a credential value
  router.put('/:key', async (req, res) => {
    const { key } = req.params;
    if (!KEY_RE.test(key)) {
      res.status(400).json({ error: 'Invalid key format — use lowercase letters, digits, and underscores only' });
      return;
    }
    const { value } = req.body as { value?: unknown };
    if (typeof value !== 'string' || value.length === 0) {
      res.status(400).json({ error: 'Body must include a non-empty "value" string' });
      return;
    }
    const { userId, tenantId } = req.caller;
    await store.set(userId, tenantId, key, value);
    res.json({ ok: true, key, userId });
  });

  // DELETE /credentials/:key — remove a credential
  router.delete('/:key', async (req, res) => {
    const { key } = req.params;
    const { userId, tenantId } = req.caller;
    const deleted = await store.delete(userId, tenantId, key);
    if (!deleted) {
      res.status(404).json({ error: `Credential "${key}" not found` });
      return;
    }
    res.json({ ok: true, key });
  });

  return router;
}
