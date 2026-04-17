import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { LabelGmailDraftUseCase } from '../../../application/use-cases/label-gmail-draft/LabelGmailDraftUseCase.js';
import { N8nAdapter } from '../../../infrastructure/adapters/n8n/N8nAdapter.js';

export function registerLabelGmailDraft(server: McpServer): void {
  const adapter = new N8nAdapter(
    process.env['N8N_BASE_URL'] ?? 'https://n8n.bagile.co.uk'
  );
  const useCase = new LabelGmailDraftUseCase(adapter);

  server.tool(
    'pa_label_gmail_draft',
    "Apply the 'Employee/Pam' label to a Gmail draft via n8n webhook. Call this after every gmail_create_draft.",
    {
      messageId: z.string().describe('Gmail message ID returned by gmail_create_draft'),
    },
    async (args) => {
      const result = await useCase.execute({ messageId: args.messageId });
      return { content: [{ type: 'text' as const, text: JSON.stringify(result, null, 2) }] };
    }
  );
}
