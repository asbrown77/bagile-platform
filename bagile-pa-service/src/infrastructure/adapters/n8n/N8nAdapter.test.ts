import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { N8nAdapter } from './N8nAdapter.js';

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

describe('N8nAdapter', () => {
  let adapter: N8nAdapter;

  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
    adapter = new N8nAdapter('https://n8n.bagile.co.uk');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('calls the correct URL with messageId in body', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, { success: true }));

    await adapter.labelGmailDraftAsPam('msg-abc-123');

    expect(fetch).toHaveBeenCalledOnce();
    const [url, init] = vi.mocked(fetch).mock.calls[0];
    expect(url).toBe('https://n8n.bagile.co.uk/webhook/label-draft-pam');
    expect(JSON.parse(init?.body as string)).toEqual({ messageId: 'msg-abc-123' });
  });

  it('uses POST method', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, {}));

    await adapter.labelGmailDraftAsPam('msg-xyz');

    const [, init] = vi.mocked(fetch).mock.calls[0];
    expect(init?.method).toBe('POST');
  });
});
