import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { TransferFooEventTicketUseCase } from '../../../application/use-cases/transfer-fooevent-ticket/TransferFooEventTicketUseCase.js';
import { PlaywrightRunner } from '../../../infrastructure/adapters/playwright/PlaywrightRunner.js';
import { buildCredentialResolver } from '../../../infrastructure/credentials/buildCredentialResolver.js';

export function registerTransferFooEventTicket(server: McpServer): void {
  const useCase = new TransferFooEventTicketUseCase(new PlaywrightRunner());

  server.tool(
    'pa_transfer_fooevent_ticket',
    'Transfer a FooEvents ticket to a new course in wp-admin. Cancels old ticket, creates new one, and resends to attendee.',
    {
      oldTicketPostId: z.number().int().positive().describe('wp-admin post ID of the old ticket to cancel'),
      newProductId: z.number().int().positive().describe('WooCommerce product ID of the new course'),
      attendeeFirstName: z.string().describe('Attendee first name'),
      attendeeLastName: z.string().describe('Attendee last name'),
      attendeeEmail: z.string().email().describe('Attendee email address'),
      attendeeCompany: z.string().optional().describe('Attendee company (optional)'),
      fromCourseCode: z.string().describe('Old course code e.g. PSPO-260326-CB'),
      toCourseCode: z.string().describe('New course code e.g. PSPO-150526-AB'),
    },
    async (args) => {
      const userId = process.env['PA_USER_ID'] ?? 'alex';
      const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
      const credentialResolver = buildCredentialResolver(userId, tenantId);
      const result = await useCase.execute({ ...args, tenantId, credentialResolver });
      return { content: [{ type: 'text' as const, text: JSON.stringify(result, null, 2) }] };
    }
  );
}
