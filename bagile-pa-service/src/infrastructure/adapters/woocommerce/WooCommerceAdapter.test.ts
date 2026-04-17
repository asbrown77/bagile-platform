import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { WooCommerceAdapter } from './WooCommerceAdapter.js';

function makeResponse(status: number, body: unknown = {}, statusText = 'OK'): Response {
  return {
    ok: status >= 200 && status < 300,
    status,
    statusText,
    json: async () => body,
    headers: {
      get: (name: string) =>
        name.toLowerCase() === 'content-type' ? 'application/json' : null,
    },
  } as unknown as Response;
}

describe('WooCommerceAdapter', () => {
  let adapter: WooCommerceAdapter;

  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
    adapter = new WooCommerceAdapter(
      'https://www.bagile.co.uk',
      'test-user',
      'test-pass'
    );
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('calls the correct URL with PUT method', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, { id: 123 }));

    await adapter.setProductOutOfStock(123);

    expect(fetch).toHaveBeenCalledOnce();
    const [url, init] = vi.mocked(fetch).mock.calls[0];
    expect(url).toBe('https://www.bagile.co.uk/wp-json/wc/v3/products/123');
    expect(init?.method).toBe('PUT');
  });

  it('sends stock_status outofstock in request body', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, { id: 456 }));

    await adapter.setProductOutOfStock(456);

    const [, init] = vi.mocked(fetch).mock.calls[0];
    expect(JSON.parse(init?.body as string)).toEqual({ stock_status: 'outofstock' });
  });

  it('includes Basic Auth header with base64 encoded credentials', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, { id: 789 }));

    await adapter.setProductOutOfStock(789);

    const [, init] = vi.mocked(fetch).mock.calls[0];
    const headers = init?.headers as Record<string, string>;
    const expectedCreds = Buffer.from('test-user:test-pass').toString('base64');
    expect(headers['Authorization']).toBe(`Basic ${expectedCreds}`);
  });
});
