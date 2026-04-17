import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { Pool } from 'pg';
import { PostgresPaTaskRepository } from '../../../infrastructure/persistence/PostgresPaTaskRepository.js';
import { PaTaskService } from '../../../application/services/PaTaskService.js';

function createService(): PaTaskService {
  const pool = new Pool({ connectionString: process.env.DATABASE_URL });
  return new PaTaskService(new PostgresPaTaskRepository(pool));
}

export function registerPaTasks(server: McpServer): void {
  const service = createService();
  const userId = process.env.PA_USER_ID ?? 'alex';

  server.tool(
    'pa_tasks_list',
    'List open PA inbox tasks',
    {},
    async () => {
      const tasks = await service.listOpen('bagile', userId);
      return { content: [{ type: 'text' as const, text: JSON.stringify(tasks, null, 2) }] };
    }
  );

  server.tool(
    'pa_tasks_complete',
    'Mark a PA task as complete',
    { id: z.string().uuid().describe('Task ID to complete') },
    async ({ id }) => {
      const task = await service.completeTask(id);
      return { content: [{ type: 'text' as const, text: JSON.stringify(task, null, 2) }] };
    }
  );
}
