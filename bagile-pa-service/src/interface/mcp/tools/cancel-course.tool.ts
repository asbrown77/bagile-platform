import type { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { z } from 'zod';
import { CancelCourseUseCase } from '../../../application/use-cases/cancel-course/CancelCourseUseCase.js';
import { WooCommerceAdapter } from '../../../infrastructure/adapters/woocommerce/WooCommerceAdapter.js';

export function registerCancelCourse(server: McpServer): void {
  const adapter = new WooCommerceAdapter(
    process.env['WC_BASE_URL'] ?? 'https://www.bagile.co.uk',
    process.env['WP_USERNAME'] ?? '',
    process.env['WP_APP_PASSWORD'] ?? ''
  );
  const useCase = new CancelCourseUseCase(adapter);

  server.tool(
    'pa_cancel_course',
    "Cancel a course by marking its WooCommerce product as out of stock (shows 'Sold Out' on website, keeps scrum.org links alive).",
    {
      productId: z.number().int().positive().describe('WooCommerce product ID to mark as out of stock'),
    },
    async (args) => {
      const result = await useCase.execute({ productId: args.productId });
      return { content: [{ type: 'text' as const, text: JSON.stringify(result, null, 2) }] };
    }
  );
}
