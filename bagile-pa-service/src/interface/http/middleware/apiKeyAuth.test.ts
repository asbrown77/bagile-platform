import { describe, it, expect, vi } from 'vitest';
import { parseApiKeys, apiKeyAuth, requireAdmin } from './apiKeyAuth.js';
import type { Request, Response, NextFunction } from 'express';

function makeReq(authHeader?: string): Partial<Request> {
  return { headers: authHeader ? { authorization: authHeader } : {} } as Partial<Request>;
}

function makeRes(): { status: ReturnType<typeof vi.fn>; json: ReturnType<typeof vi.fn>; _status: number } {
  const res = { _status: 200, status: vi.fn(), json: vi.fn() };
  res.status.mockReturnValue(res);
  return res as any;
}

describe('parseApiKeys', () => {
  it('parses valid entries', () => {
    const map = parseApiKeys('pak_abc:portal:alex:bagile:admin,pak_xyz:chatgpt:alex:bagile:reader');
    expect(map.size).toBe(2);
    expect(map.get('pak_abc')).toEqual({ name: 'portal', userId: 'alex', tenantId: 'bagile', role: 'admin' });
    expect(map.get('pak_xyz')).toEqual({ name: 'chatgpt', userId: 'alex', tenantId: 'bagile', role: 'reader' });
  });

  it('ignores entries with wrong field count', () => {
    const map = parseApiKeys('bad_entry,pak_abc:portal:alex:bagile:admin');
    expect(map.size).toBe(1);
  });

  it('ignores entries with invalid role', () => {
    const map = parseApiKeys('pak_abc:portal:alex:bagile:superuser');
    expect(map.size).toBe(0);
  });

  it('returns empty map for empty string', () => {
    expect(parseApiKeys('').size).toBe(0);
  });
});

describe('apiKeyAuth middleware', () => {
  const keys = parseApiKeys('pak_admin:portal:alex:bagile:admin,pak_reader:chatgpt:alex:bagile:reader');
  const middleware = apiKeyAuth(keys);

  it('passes with valid admin key', () => {
    const req = makeReq('Bearer pak_admin') as any;
    const res = makeRes();
    const next: NextFunction = vi.fn();
    middleware(req, res as any, next);
    expect(next).toHaveBeenCalled();
    expect(req.caller).toEqual({ name: 'portal', userId: 'alex', tenantId: 'bagile', role: 'admin' });
  });

  it('passes with valid reader key', () => {
    const req = makeReq('Bearer pak_reader') as any;
    const res = makeRes();
    const next: NextFunction = vi.fn();
    middleware(req, res as any, next);
    expect(next).toHaveBeenCalled();
    expect(req.caller.role).toBe('reader');
  });

  it('returns 401 for missing header', () => {
    const req = makeReq() as any;
    const res = makeRes();
    const next: NextFunction = vi.fn();
    middleware(req, res as any, next);
    expect(res.status).toHaveBeenCalledWith(401);
    expect(next).not.toHaveBeenCalled();
  });

  it('returns 401 for unknown key', () => {
    const req = makeReq('Bearer unknown') as any;
    const res = makeRes();
    const next: NextFunction = vi.fn();
    middleware(req, res as any, next);
    expect(res.status).toHaveBeenCalledWith(401);
  });

  it('returns 401 for non-Bearer scheme', () => {
    const req = makeReq('Basic somebase64') as any;
    const res = makeRes();
    const next: NextFunction = vi.fn();
    middleware(req, res as any, next);
    expect(res.status).toHaveBeenCalledWith(401);
  });
});

describe('requireAdmin middleware', () => {
  it('passes for admin caller', () => {
    const req = { caller: { role: 'admin' } } as any;
    const res = makeRes();
    const next: NextFunction = vi.fn();
    requireAdmin(req, res as any, next);
    expect(next).toHaveBeenCalled();
  });

  it('returns 403 for reader caller', () => {
    const req = { caller: { role: 'reader' } } as any;
    const res = makeRes();
    const next: NextFunction = vi.fn();
    requireAdmin(req, res as any, next);
    expect(res.status).toHaveBeenCalledWith(403);
    expect(next).not.toHaveBeenCalled();
  });
});
