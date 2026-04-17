import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { LabelGmailDraftUseCase } from '../../../application/use-cases/label-gmail-draft/LabelGmailDraftUseCase.js';
import { N8nAdapter } from '../../../infrastructure/adapters/n8n/N8nAdapter.js';
import { buildCompanySettingsResolver } from '../../../infrastructure/credentials/buildCompanySettingsResolver.js';

export function registerLabelGmailDraft(server: McpServer): void {
  server.tool(
    'pa_label_gmail_draft',
    "Apply the 'Employee/Pam' label to a Gmail draft via n8n webhook. Call this after every gmail_create_draft.",
    {
      messageId: z.string().describe('Gmail message ID returned by gmail_create_draft'),
    },
    async (args) => {
      const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
      const company = buildCompanySettingsResolver(tenantId);
      const n8nBaseUrl = (await company('n8n_base_url')) ?? 'https://n8n.bagile.co.uk';
      const useCase = new LabelGmailDraftUseCase(new N8nAdapter(n8nBaseUrl));
      const result = await useCase.execute({ messageId: args.messageId });
      return { content: [{ type: 'text' as const, text: JSON.stringify(result, null, 2) }] };
    }
  );
}
