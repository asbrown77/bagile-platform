import type { IWooCommercePort } from '../../../domain/ports/IWooCommercePort.js';
import { fetchJson } from '../../http/fetchJson.js';

export class WooCommerceAdapter implements IWooCommercePort {
  constructor(
    private readonly baseUrl: string,
    private readonly username: string,
    private readonly appPassword: string
  ) {}

  async setProductOutOfStock(productId: number): Promise<void> {
    const credentials = Buffer.from(`${this.username}:${this.appPassword}`).toString('base64');
    await fetchJson(`${this.baseUrl}/wp-json/wc/v3/products/${productId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Basic ${credentials}`,
      },
      body: JSON.stringify({ stock_status: 'outofstock' }),
    });
  }
}
