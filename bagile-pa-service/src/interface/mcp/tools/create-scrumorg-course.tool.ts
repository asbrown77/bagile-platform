import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { CreateScrumOrgCourseUseCase } from '../../../application/use-cases/create-scrumorg-course/CreateScrumOrgCourseUseCase.js';
import { PlaywrightRunner } from '../../../infrastructure/adapters/playwright/PlaywrightRunner.js';
import { buildCredentialResolver } from '../../../infrastructure/credentials/buildCredentialResolver.js';

export function registerCreateScrumOrgCourse(server: McpServer): void {
  const useCase = new CreateScrumOrgCourseUseCase(new PlaywrightRunner());

  server.tool(
    'pa_create_scrumorg_course',
    'Create a new course listing on scrum.org by copying the most recent course of the same type for the trainer, then updating dates and registration URL.',
    {
      courseType: z.string().describe('Course type e.g. PSM, PSPO, PSK, PALE'),
      trainerName: z.string().describe('Trainer full name e.g. Chris Bexon'),
      startDate: z.string().describe('Start date YYYY-MM-DD'),
      endDate: z.string().describe('End date YYYY-MM-DD'),
      registrationUrl: z.string().url().describe('WooCommerce product URL for the course'),
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
