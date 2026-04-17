import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { Pool } from 'pg';
import { PostgresHealthRepository } from '../../../infrastructure/persistence/PostgresHealthRepository.js';
import { StubAlertAdapter } from '../../../infrastructure/alerts/StubAlertAdapter.js';
import { HealthService } from '../../../application/services/HealthService.js';

function createService(): HealthService {
  const pool = new Pool({ connectionString: process.env.DATABASE_URL });
  return new HealthService(new PostgresHealthRepository(pool), new StubAlertAdapter());
}

export function registerHealthStatus(server: McpServer): void {
  const service = createService();

  server.tool(
    'pa_health_status',
    'Get health status of all PA automations — shows last run result per automation',
    {},
    async () => {
      const tenantId = process.env.PA_TENANT_ID ?? 'bagile';
      const records = await service.getStatus(tenantId);
      return { content: [{ type: 'text' as const, text: JSON.stringify(records, null, 2) }] };
    }
  );
}
