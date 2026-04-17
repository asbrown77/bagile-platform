import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { XeroAdapter } from './XeroAdapter.js';

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

const SAMPLE_API_INVOICE = {
  InvoiceID: 'uuid-001',
  InvoiceNumber: 'INV-0042',
  Contact: { Name: 'Acme Corp' },
  Total: 1140,
  Status: 'AUTHORISED',
};

describe('XeroAdapter', () => {
  let adapter: XeroAdapter;

  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
    adapter = new XeroAdapter('test-access-token', 'test-tenant-id');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('returns invoice when found by invoice number', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse(200, { Invoices: [SAMPLE_API_INVOICE] }));

    const result = await adapter.findInvoice('INV-0042');

    expect(result).toEqual({
      invoiceId: 'uuid-001',
      invoiceNumber: 'INV-0042',
      contactName: 'Acme Corp',
      total: 1140,
      status: 'AUTHORISED',
    });

    const [url, init] = vi.mocked(fetch).mock.calls[0];
    expect(url).toContain('InvoiceNumbers=INV-0042');
    const headers = init?.headers as Record<string, string>;
    expect(headers['Authorization']).toBe('Bearer test-access-token');
    expect(headers['xero-tenant-id']).toBe('test-tenant-id');
  });

  it('returns invoice when found by contact name (number search returns empty)', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse(200, { Invoices: [] }))            // number search — miss
      .mockResolvedValueOnce(makeResponse(200, { Invoices: [SAMPLE_API_INVOICE] })); // contact search — hit

    const result = await adapter.findInvoice('Acme Corp');

    expect(result).not.toBeNull();
    expect(result?.contactName).toBe('Acme Corp');

    const contactSearchUrl = vi.mocked(fetch).mock.calls[1][0] as string;
    expect(contactSearchUrl).toContain('where=');
    expect(contactSearchUrl).toContain('Acme');
  });

  it('returns null when neither search finds anything', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse(200, { Invoices: [] }))
      .mockResolvedValueOnce(makeResponse(200, { Invoices: [] }));

    const result = await adapter.findInvoice('Nobody Corp');

    expect(result).toBeNull();
  });

  it('sanitises contact name by removing quotes to prevent injection', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse(200, { Invoices: [] }))
      .mockResolvedValueOnce(makeResponse(200, { Invoices: [] }));

    await adapter.findInvoice('O"Malley Corp');

    const contactSearchUrl = vi.mocked(fetch).mock.calls[1][0] as string;
    expect(decodeURIComponent(contactSearchUrl)).not.toContain('O"Malley');
    expect(decodeURIComponent(contactSearchUrl)).toContain('OMalley');
  });
});
