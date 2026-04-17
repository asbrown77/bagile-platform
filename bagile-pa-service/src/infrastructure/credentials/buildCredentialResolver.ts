import { Pool } from 'pg';
import { PostgresCredentialStore } from './PostgresCredentialStore.js';
import type { CredentialResolver, ICredentialStore } from '../../domain/ports/ICredentialStore.js';

let _store: PostgresCredentialStore | null = null;

/**
 * Returns a singleton credential store backed by Postgres, or null if
 * DATABASE_URL or PA_ENCRYPTION_KEY is not configured.
 */
export function getCredentialStore(): ICredentialStore | null {
  if (_store) return _store;
  const dbUrl = process.env['DATABASE_URL'];
  const encKey = process.env['PA_ENCRYPTION_KEY'];
  if (!dbUrl || !encKey) return null;
  try {
    _store = new PostgresCredentialStore(new Pool({ connectionString: dbUrl }));
    return _store;
  } catch {
    return null;
  }
}

/**
 * Builds a CredentialResolver bound to a specific user, falling back to
 * env vars if the DB store is unavailable. Used by MCP tool handlers.
 *
 * Env var fallback map: key → ENV_VAR_NAME
 */
const ENV_FALLBACKS: Record<string, string> = {
  scrumorg_username: 'SCRUMORG_USERNAME',
  scrumorg_password: 'SCRUMORG_PASSWORD',
  trello_api_key: 'TRELLO_API_KEY',
  trello_token: 'TRELLO_TOKEN',
  wp_username: 'WP_USERNAME',
  wp_app_password: 'WP_APP_PASSWORD',
};

export function buildCredentialResolver(userId: string, tenantId: string): CredentialResolver {
  const store = getCredentialStore();
  return async (key: string): Promise<string | undefined> => {
    if (store) {
      const val = await store.get(userId, tenantId, key).catch(() => undefined);
      if (val !== undefined) return val;
    }
    const envKey = ENV_FALLBACKS[key];
    return envKey ? (process.env[envKey] ?? undefined) : undefined;
  };
}
