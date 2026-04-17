import { Router } from 'express';
import { Pool } from 'pg';
import { PostgresCompanySettingsStore } from '../../../infrastructure/credentials/PostgresCompanySettingsStore.js';
import { requireAdmin } from '../middleware/apiKeyAuth.js';

const KEY_RE = /^[a-z0-9_]{1,100}$/;

export function createCompanySettingsRouter(pool: Pool): Router {
  const router = Router();
  const store = new PostgresCompanySettingsStore(pool);

  router.use(requireAdmin);

  // GET /company-settings — list stored key names for the tenant
  router.get('/', async (req, res) => {
    const { tenantId } = req.caller;
    const keys = await store.listKeys(tenantId);
    res.json({ keys, tenantId });
  });

  // PUT /company-settings/:key — upsert a setting value
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
    const { tenantId } = req.caller;
    await store.set(tenantId, key, value);
    res.json({ ok: true, key, tenantId });
  });

  // DELETE /company-settings/:key
  router.delete('/:key', async (req, res) => {
    const { key } = req.params;
    const { tenantId } = req.caller;
    const deleted = await store.delete(tenantId, key);
    if (!deleted) {
      res.status(404).json({ error: `Setting "${key}" not found` });
      return;
    }
    res.json({ ok: true, key });
  });

  return router;
}
