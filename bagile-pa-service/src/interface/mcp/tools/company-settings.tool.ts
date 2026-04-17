import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { getCompanySettingsStore } from '../../../infrastructure/credentials/buildCompanySettingsResolver.js';

const KNOWN_KEYS = [
  'bagile_api_url', 'bagile_api_key',
  'n8n_base_url',
  'wc_base_url', 'wp_admin_url',
  'xero_client_id', 'xero_client_secret', 'xero_tenant_id', 'xero_refresh_token',
];

export function registerCompanySettings(server: McpServer): void {
  const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';

  server.tool(
    'pa_set_company_setting',
    `Store a company-wide setting in the encrypted settings store. Known keys: ${KNOWN_KEYS.join(', ')}. Values are encrypted at rest and shared across all users of the tenant.`,
    {
      key: z.string().describe('Setting key e.g. xero_client_id'),
      value: z.string().describe('Setting value to store'),
    },
    async ({ key, value }) => {
      const store = getCompanySettingsStore();
      if (!store) {
        return {
          content: [{
            type: 'text' as const,
            text: JSON.stringify({ error: 'Settings store unavailable — DATABASE_URL or PA_ENCRYPTION_KEY not configured' }),
          }],
        };
      }
      await store.set(tenantId, key, value);
      return {
        content: [{ type: 'text' as const, text: JSON.stringify({ ok: true, key, tenantId }) }],
      };
    }
  );

  server.tool(
    'pa_list_company_settings',
    'List which company setting keys are stored. Values are never returned.',
    {},
    async () => {
      const store = getCompanySettingsStore();
      if (!store) {
        return {
          content: [{
            type: 'text' as const,
            text: JSON.stringify({ keys: [], note: 'Settings store unavailable' }),
          }],
        };
      }
      const keys = await store.listKeys(tenantId);
      return {
        content: [{ type: 'text' as const, text: JSON.stringify({ keys, tenantId }) }],
      };
    }
  );

  server.tool(
    'pa_delete_company_setting',
    'Remove a company setting.',
    { key: z.string().describe('Setting key to delete') },
    async ({ key }) => {
      const store = getCompanySettingsStore();
      if (!store) {
        return { content: [{ type: 'text' as const, text: JSON.stringify({ error: 'Settings store unavailable' }) }] };
      }
      const deleted = await store.delete(tenantId, key);
      return { content: [{ type: 'text' as const, text: JSON.stringify({ ok: deleted, key }) }] };
    }
  );
}
