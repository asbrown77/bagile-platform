import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { LookupXeroInvoiceUseCase } from '../../../application/use-cases/lookup-xero-invoice/LookupXeroInvoiceUseCase.js';
import { XeroAdapter } from '../../../infrastructure/adapters/xero/XeroAdapter.js';
import { XeroTokenManager } from '../../../infrastructure/adapters/xero/XeroTokenManager.js';
import { buildCompanySettingsResolver } from '../../../infrastructure/credentials/buildCompanySettingsResolver.js';

export function registerLookupXeroInvoice(server: McpServer): void {
  server.tool(
    'pa_lookup_xero_invoice',
    'Find a Xero invoice by invoice number or contact name. Returns invoice details and online URL.',
    {
      query: z.string().describe('Invoice number (e.g. INV-1234) or contact/company name'),
    },
    async (args) => {
      const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
      const company = buildCompanySettingsResolver(tenantId);
      const clientId     = (await company('xero_client_id'))     ?? '';
      const clientSecret = (await company('xero_client_secret')) ?? '';
      const refreshToken = (await company('xero_refresh_token')) ?? '';
      const xeroTenantId = (await company('xero_tenant_id'))     ?? 'aef46d85-ec9c-475b-990d-5480d708605c';
      const tokenManager = new XeroTokenManager(clientId, clientSecret, refreshToken);
      const adapter = new XeroAdapter(() => tokenManager.getToken(), xeroTenantId);
      const useCase = new LookupXeroInvoiceUseCase(adapter);
      const result = await useCase.execute({ query: args.query });
      return { content: [{ type: 'text' as const, text: JSON.stringify(result, null, 2) }] };
    }
  );
}
