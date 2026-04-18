import { Pool } from 'pg';
import { PostgresCredentialStore } from './PostgresCredentialStore.js';
import type { CredentialResolver, ICredentialStore } from '../../domain/ports/ICredentialStore.js';

let _pool: Pool | null = null;
let _store: PostgresCredentialStore | null = null;

function getPool(): Pool | null {
  if (_pool) return _pool;
  const dbUrl = process.env['DATABASE_URL'];
  if (!dbUrl) return null;
  try {
    _pool = new Pool({ connectionString: dbUrl });
    return _pool;
  } catch {
    return null;
  }
}

/**
 * Returns a singleton credential store backed by Postgres, or null if
 * DATABASE_URL or PA_ENCRYPTION_KEY is not configured.
 */
export function getCredentialStore(): ICredentialStore | null {
  if (_store) return _store;
  const encKey = process.env['PA_ENCRYPTION_KEY'];
  const pool = getPool();
  if (!pool || !encKey) return null;
  try {
    _store = new PostgresCredentialStore(pool);
    return _store;
  } catch {
    return null;
  }
}

/**
 * Look up a key in bagile.service_config (tenant-level, unencrypted).
 * Used as a fallback for service-level settings like session cookies that
 * are stored via the API admin endpoint rather than per-user credentials.
 */
async function getServiceConfig(key: string): Promise<string | undefined> {
  const pool = getPool();
  if (!pool) {
    console.error(`[credentials] getServiceConfig(${key}): no pool — DATABASE_URL missing or pool init failed`);
    return undefined;
  }
  try {
    const { rows } = await pool.query<{ value: string }>(
      `SELECT value FROM bagile.service_config WHERE key = $1`,
      [key],
    );
    return rows[0]?.value ?? undefined;
  } catch (err) {
    console.error(`[credentials] getServiceConfig(${key}): DB query failed — ${(err as Error).message}`);
    return undefined;
  }
}

/**
 * Builds a CredentialResolver bound to a specific user, falling back to
 * service_config then env vars if the DB store is unavailable.
 * Used by MCP tool handlers.
 *
 * Lookup order:
 *   1. bagile.pa_user_credentials (per-user, encrypted)
 *   2. bagile.service_config (tenant-level, plain — for session cookies etc.)
 *   3. ENV_FALLBACKS (process environment)
 */
const ENV_FALLBACKS: Record<string, string> = {
  scrumorg_username: 'SCRUMORG_USERNAME',
  scrumorg_password: 'SCRUMORG_PASSWORD',
  scrumorg_session_cookies: 'SCRUMORG_SESSION_COOKIES',
  trello_api_key: 'TRELLO_API_KEY',
  trello_token: 'TRELLO_TOKEN',
  wp_username: 'WP_USERNAME',
  wp_app_password: 'WP_APP_PASSWORD',
};

export function buildCredentialResolver(userId: string, tenantId: string): CredentialResolver {
  const store = getCredentialStore();
  return async (key: string): Promise<string | undefined> => {
    // 1. Per-user encrypted credentials
    if (store) {
      const val = await store.get(userId, tenantId, key).catch(() => undefined);
      if (val !== undefined) return val;
    }
    // 2. Tenant-level service config (e.g. scrumorg_session_cookies)
    const svcVal = await getServiceConfig(key);
    if (svcVal !== undefined) return svcVal;
    // 3. Environment variable fallback
    const envKey = ENV_FALLBACKS[key];
    return envKey ? (process.env[envKey] ?? undefined) : undefined;
  };
}
