import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { BagileApiAdapter } from '../../../infrastructure/adapters/bagile-api/BagileApiAdapter.js';

export function registerCoursesAtRisk(server: McpServer): void {
  server.tool(
    'pa_courses_at_risk',
    'List public courses that are below minimum enrolment and may need to be cancelled. Returns course code, title, start date, current enrolments, minimum required, and recommended action.',
    {
      daysAhead: z.number().int().min(1).max(90).default(30).describe('How many days ahead to look for at-risk courses (default 30)'),
    },
    async (args) => {
      const baseUrl = process.env['BAGILE_API_URL'] ?? 'https://api.bagile.co.uk';
      const apiKey = process.env['BAGILE_API_KEY'] ?? '';
      const adapter = new BagileApiAdapter(baseUrl, apiKey);
      const courses = await adapter.getCoursesAtRisk(args.daysAhead);
      const text = courses.length === 0
        ? 'No at-risk courses found in the next ' + args.daysAhead + ' days.'
        : JSON.stringify(courses, null, 2);
      return { content: [{ type: 'text' as const, text }] };
    }
  );
}
