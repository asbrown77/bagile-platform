import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { TrelloWriteAdapter } from './TrelloWriteAdapter.js';

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

describe('TrelloWriteAdapter', () => {
  let adapter: TrelloWriteAdapter;

  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
    adapter = new TrelloWriteAdapter('test-key', 'test-token');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('moves card when listId is provided', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, { id: 'card-abc' }));

    const result = await adapter.updateCard({ cardId: 'card-abc', listId: 'list-123' });

    expect(fetch).toHaveBeenCalledOnce();
    const [url, init] = vi.mocked(fetch).mock.calls[0];
    expect(url).toContain('/cards/card-abc');
    expect(url).toContain('key=test-key');
    expect(url).toContain('token=test-token');
    expect(init?.method).toBe('PUT');
    expect(JSON.parse(init?.body as string)).toEqual({ idList: 'list-123' });
    expect(result.moved).toBe(true);
    expect(result.commented).toBe(false);
    expect(result.dueUpdated).toBe(false);
  });

  it('adds comment when comment is provided', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, {}));

    const result = await adapter.updateCard({ cardId: 'card-abc', comment: 'Follow up sent.' });

    expect(fetch).toHaveBeenCalledOnce();
    const [url, init] = vi.mocked(fetch).mock.calls[0];
    expect(url).toContain('/cards/card-abc/actions/comments');
    expect(init?.method).toBe('POST');
    expect(JSON.parse(init?.body as string)).toEqual({ text: 'Follow up sent.' });
    expect(result.moved).toBe(false);
    expect(result.commented).toBe(true);
    expect(result.dueUpdated).toBe(false);
  });

  it('updates due date when dueDate is provided', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, { id: 'card-abc' }));

    const result = await adapter.updateCard({
      cardId: 'card-abc',
      dueDate: '2026-04-20T09:00:00.000Z',
    });

    expect(fetch).toHaveBeenCalledOnce();
    const [url, init] = vi.mocked(fetch).mock.calls[0];
    expect(url).toContain('/cards/card-abc');
    expect(init?.method).toBe('PUT');
    expect(JSON.parse(init?.body as string)).toEqual({ due: '2026-04-20T09:00:00.000Z' });
    expect(result.dueUpdated).toBe(true);
  });

  it('clears due date when dueDate is null', async () => {
    vi.mocked(fetch).mockResolvedValue(makeResponse(200, { id: 'card-abc' }));

    const result = await adapter.updateCard({ cardId: 'card-abc', dueDate: null });

    const [, init] = vi.mocked(fetch).mock.calls[0];
    expect(JSON.parse(init?.body as string)).toEqual({ due: null });
    expect(result.dueUpdated).toBe(true);
  });

  it('performs all three operations when all fields provided', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeResponse(200, { id: 'card-abc' })) // move
      .mockResolvedValueOnce(makeResponse(200, {}))                  // comment
      .mockResolvedValueOnce(makeResponse(200, { id: 'card-abc' })); // due

    const result = await adapter.updateCard({
      cardId: 'card-abc',
      listId: 'list-xyz',
      comment: 'Moved.',
      dueDate: '2026-05-01T00:00:00.000Z',
    });

    expect(fetch).toHaveBeenCalledTimes(3);
    expect(result).toEqual({
      cardId: 'card-abc',
      moved: true,
      commented: true,
      dueUpdated: true,
    });
  });

  it('skips all operations when only cardId is provided', async () => {
    const result = await adapter.updateCard({ cardId: 'card-abc' });

    expect(fetch).not.toHaveBeenCalled();
    expect(result).toEqual({
      cardId: 'card-abc',
      moved: false,
      commented: false,
      dueUpdated: false,
    });
  });
});
