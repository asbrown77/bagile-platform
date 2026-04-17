import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { LookupXeroInvoiceUseCase } from '../../../application/use-cases/lookup-xero-invoice/LookupXeroInvoiceUseCase.js';
import { XeroAdapter } from '../../../infrastructure/adapters/xero/XeroAdapter.js';
import { XeroTokenManager } from '../../../infrastructure/adapters/xero/XeroTokenManager.js';

function createUseCase(): LookupXeroInvoiceUseCase {
  const clientId = process.env['XERO_CLIENT_ID'] ?? '';
  const clientSecret = process.env['XERO_CLIENT_SECRET'] ?? '';
  const refreshToken = process.env['XERO_REFRESH_TOKEN'] ?? '';
  const tenantId = process.env['XERO_TENANT_ID'] ?? 'aef46d85-ec9c-475b-990d-5480d708605c';

  const tokenManager = new XeroTokenManager(clientId, clientSecret, refreshToken);
  const adapter = new XeroAdapter(() => tokenManager.getToken(), tenantId);
  return new LookupXeroInvoiceUseCase(adapter);
}

export function registerLookupXeroInvoice(server: McpServer): void {
  const useCase = createUseCase();

  server.tool(
    'pa_lookup_xero_invoice',
    'Find a Xero invoice by invoice number or contact name. Returns invoice details and online URL.',
    {
      query: z.string().describe('Invoice number (e.g. INV-1234) or contact/company name'),
    },
    async (args) => {
      const result = await useCase.execute({ query: args.query });
      return { content: [{ type: 'text' as const, text: JSON.stringify(result, null, 2) }] };
    }
  );
}
