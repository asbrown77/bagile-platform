import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { getCredentialStore } from '../../../infrastructure/credentials/buildCredentialResolver.js';

const KNOWN_KEYS = [
  'scrumorg_username',
  'scrumorg_password',
  'trello_api_key',
  'trello_token',
  'wp_username',
  'wp_app_password',
];

export function registerCredentials(server: McpServer): void {
  const userId = process.env['PA_USER_ID'] ?? 'alex';
  const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';

  server.tool(
    'pa_set_credential',
    `Store a personal credential in the encrypted credential store. Known keys: ${KNOWN_KEYS.join(', ')}. Values are encrypted at rest.`,
    {
      key: z.string().describe('Credential key e.g. scrumorg_username'),
      value: z.string().describe('Credential value to store'),
    },
    async ({ key, value }) => {
      const store = getCredentialStore();
      if (!store) {
        return {
          content: [{
            type: 'text' as const,
            text: JSON.stringify({ error: 'Credential store unavailable — DATABASE_URL or PA_ENCRYPTION_KEY not configured' }),
          }],
        };
      }
      await store.set(userId, tenantId, key, value);
      return {
        content: [{ type: 'text' as const, text: JSON.stringify({ ok: true, key, userId }) }],
      };
    }
  );

  server.tool(
    'pa_list_credentials',
    'List which credential keys are stored for the current user. Values are never returned.',
    {},
    async () => {
      const store = getCredentialStore();
      if (!store) {
        return {
          content: [{
            type: 'text' as const,
            text: JSON.stringify({ keys: [], note: 'Credential store unavailable — DATABASE_URL or PA_ENCRYPTION_KEY not configured' }),
          }],
        };
      }
      const keys = await store.listKeys(userId, tenantId);
      return {
        content: [{ type: 'text' as const, text: JSON.stringify({ keys, userId }) }],
      };
    }
  );

  server.tool(
    'pa_delete_credential',
    'Remove a stored credential for the current user.',
    {
      key: z.string().describe('Credential key to delete'),
    },
    async ({ key }) => {
      const store = getCredentialStore();
      if (!store) {
        return {
          content: [{
            type: 'text' as const,
            text: JSON.stringify({ error: 'Credential store unavailable' }),
          }],
        };
      }
      const deleted = await store.delete(userId, tenantId, key);
      return {
        content: [{ type: 'text' as const, text: JSON.stringify({ ok: deleted, key }) }],
      };
    }
  );
}
