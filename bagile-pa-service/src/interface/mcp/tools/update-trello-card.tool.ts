import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { UpdateTrelloCardUseCase } from '../../../application/use-cases/update-trello-card/UpdateTrelloCardUseCase.js';
import { TrelloWriteAdapter } from '../../../infrastructure/adapters/trello/TrelloWriteAdapter.js';
import { buildCredentialResolver } from '../../../infrastructure/credentials/buildCredentialResolver.js';

export function registerUpdateTrelloCard(server: McpServer): void {
  server.tool(
    'pa_update_trello_card',
    "Update a Trello CRM card — move to a new list, add a comment, or update the due date. Provide at least one of listId, comment, or dueDate.",
    {
      cardId: z.string().describe('Trello card ID'),
      listId: z.string().optional().describe('List ID to move the card to'),
      comment: z.string().optional().describe('Comment text to add'),
      dueDate: z.string().optional().describe('Due date ISO string, or empty string to clear'),
    },
    async (args) => {
      const userId = process.env['PA_USER_ID'] ?? 'alex';
      const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
      const resolver = buildCredentialResolver(userId, tenantId);
      const apiKey = (await resolver('trello_api_key')) ?? '';
      const token = (await resolver('trello_token')) ?? '';
      const useCase = new UpdateTrelloCardUseCase(new TrelloWriteAdapter(apiKey, token));
      const result = await useCase.execute({
        cardId: args.cardId,
        listId: args.listId,
        comment: args.comment,
        dueDate: args.dueDate === '' ? null : args.dueDate,
      });
      return { content: [{ type: 'text' as const, text: JSON.stringify(result, null, 2) }] };
    }
  );
}
