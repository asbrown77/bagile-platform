import { Pool } from 'pg';
import { PostgresCompanySettingsStore } from './PostgresCompanySettingsStore.js';
import type { ICompanySettingsStore, CompanySettingsResolver } from '../../domain/ports/ICompanySettingsStore.js';

let _store: PostgresCompanySettingsStore | null = null;

export function getCompanySettingsStore(): ICompanySettingsStore | null {
  if (_store) return _store;
  const dbUrl = process.env['DATABASE_URL'];
  const encKey = process.env['PA_ENCRYPTION_KEY'];
  if (!dbUrl || !encKey) return null;
  try {
    _store = new PostgresCompanySettingsStore(new Pool({ connectionString: dbUrl }));
    return _store;
  } catch {
    return null;
  }
}

/**
 * Maps company setting keys to their env var fallbacks.
 * DB takes precedence; env var used when DB is unavailable or key not set.
 */
const ENV_FALLBACKS: Record<string, string> = {
  bagile_api_url:        'BAGILE_API_URL',
  bagile_api_key:        'BAGILE_API_KEY',
  n8n_base_url:          'N8N_BASE_URL',
  wc_base_url:           'WC_BASE_URL',
  wp_admin_url:          'WP_ADMIN_URL',
  xero_client_id:        'XERO_CLIENT_ID',
  xero_client_secret:    'XERO_CLIENT_SECRET',
  xero_tenant_id:        'XERO_TENANT_ID',
  xero_refresh_token:    'XERO_REFRESH_TOKEN',
};

export function buildCompanySettingsResolver(tenantId: string): CompanySettingsResolver {
  const store = getCompanySettingsStore();
  return async (key: string): Promise<string | undefined> => {
    if (store) {
      const val = await store.get(tenantId, key).catch(() => undefined);
      if (val !== undefined) return val;
    }
    const envKey = ENV_FALLBACKS[key];
    return envKey ? (process.env[envKey] ?? undefined) : undefined;
  };
}
