import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { CancelCourseUseCase } from '../../../application/use-cases/cancel-course/CancelCourseUseCase.js';
import { WooCommerceAdapter } from '../../../infrastructure/adapters/woocommerce/WooCommerceAdapter.js';
import { buildCompanySettingsResolver } from '../../../infrastructure/credentials/buildCompanySettingsResolver.js';
import { buildCredentialResolver } from '../../../infrastructure/credentials/buildCredentialResolver.js';

export function registerCancelCourse(server: McpServer): void {
  server.tool(
    'pa_cancel_course',
    "Cancel a course by marking its WooCommerce product as out of stock (shows 'Sold Out' on website, keeps scrum.org links alive).",
    {
      productId: z.number().int().positive().describe('WooCommerce product ID to mark as out of stock'),
    },
    async (args) => {
      const userId = process.env['PA_USER_ID'] ?? 'alex';
      const tenantId = process.env['PA_TENANT_ID'] ?? 'bagile';
      const company = buildCompanySettingsResolver(tenantId);
      const personal = buildCredentialResolver(userId, tenantId);
      const wcBaseUrl = (await company('wc_base_url')) ?? 'https://www.bagile.co.uk';
      const wpUsername = (await personal('wp_username')) ?? '';
      const wpPassword = (await personal('wp_app_password')) ?? '';
      const useCase = new CancelCourseUseCase(new WooCommerceAdapter(wcBaseUrl, wpUsername, wpPassword));
      const result = await useCase.execute({ productId: args.productId });
      return { content: [{ type: 'text' as const, text: JSON.stringify(result, null, 2) }] };
    }
  );
}
