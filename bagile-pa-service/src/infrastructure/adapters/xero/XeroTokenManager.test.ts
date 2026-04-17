import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { XeroTokenManager } from './XeroTokenManager.js';

function makeTokenResponse(accessToken: string, expiresIn = 1800): Response {
  return {
    ok: true,
    status: 200,
    json: async () => ({ access_token: accessToken, expires_in: expiresIn }),
  } as unknown as Response;
}

function makeErrorResponse(status: number): Response {
  return { ok: false, status, statusText: 'Unauthorized' } as unknown as Response;
}

describe('XeroTokenManager', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', vi.fn());
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('fetches a new token on first call', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeTokenResponse('token-abc'));
    const manager = new XeroTokenManager('client-id', 'client-secret', 'refresh-token');

    const token = await manager.getToken();

    expect(token).toBe('token-abc');
    expect(fetch).toHaveBeenCalledOnce();
    const [url, init] = vi.mocked(fetch).mock.calls[0];
    expect(url).toBe('https://identity.xero.com/connect/token');
    expect((init?.body as string)).toContain('grant_type=refresh_token');
    expect((init?.body as string)).toContain('refresh_token=refresh-token');
  });

  it('returns cached token on second call without re-fetching', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeTokenResponse('token-abc', 1800));
    const manager = new XeroTokenManager('client-id', 'client-secret', 'refresh-token');

    await manager.getToken();
    const token = await manager.getToken();

    expect(token).toBe('token-abc');
    expect(fetch).toHaveBeenCalledOnce();
  });

  it('refreshes when cached token has expired', async () => {
    vi.mocked(fetch)
      .mockResolvedValueOnce(makeTokenResponse('token-first', 1))  // expires in 1s, minus 60s buffer → already expired
      .mockResolvedValueOnce(makeTokenResponse('token-second', 1800));

    const manager = new XeroTokenManager('client-id', 'client-secret', 'refresh-token');

    await manager.getToken();
    const token = await manager.getToken();

    expect(token).toBe('token-second');
    expect(fetch).toHaveBeenCalledTimes(2);
  });

  it('throws when token refresh fails', async () => {
    vi.mocked(fetch).mockResolvedValueOnce(makeErrorResponse(401));
    const manager = new XeroTokenManager('client-id', 'client-secret', 'bad-refresh-token');

    await expect(manager.getToken()).rejects.toThrow('Xero token refresh failed: HTTP 401');
  });
});
