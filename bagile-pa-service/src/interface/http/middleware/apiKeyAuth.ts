import { Request, Response, NextFunction } from 'express';

export type CallerRole = 'admin' | 'reader';

export interface CallerIdentity {
  name: string;
  userId: string;
  tenantId: string;
  role: CallerRole;
}

declare global {
  // eslint-disable-next-line @typescript-eslint/no-namespace
  namespace Express {
    interface Request {
      caller: CallerIdentity;
    }
  }
}

/**
 * Parse PA_API_KEYS env var into a lookup map.
 * Format: comma-separated entries of "key:callerName:userId:tenantId:role"
 * Example: "pak_abc123:portal:alex:bagile:admin,pak_xyz:chatgpt:alex:bagile:reader"
 */
export function parseApiKeys(raw: string): Map<string, CallerIdentity> {
  const map = new Map<string, CallerIdentity>();
  for (const entry of raw.split(',')) {
    const trimmed = entry.trim();
    if (!trimmed) continue;
    const parts = trimmed.split(':');
    if (parts.length !== 5) continue;
    const [key, name, userId, tenantId, role] = parts;
    if (role !== 'admin' && role !== 'reader') continue;
    map.set(key, { name, userId, tenantId, role });
  }
  return map;
}

/**
 * Middleware: verifies Bearer token against PA_API_KEYS map.
 * Attaches req.caller on success.
 */
export function apiKeyAuth(keys: Map<string, CallerIdentity>) {
  return (req: Request, res: Response, next: NextFunction): void => {
    const authHeader = req.headers['authorization'];
    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      res.status(401).json({ error: 'Missing or invalid Authorization header' });
      return;
    }
    const token = authHeader.slice(7);
    const caller = keys.get(token);
    if (!caller) {
      res.status(401).json({ error: 'Invalid API key' });
      return;
    }
    req.caller = caller;
    next();
  };
}

/**
 * Middleware: restricts route to callers with 'admin' role.
 * Apply after apiKeyAuth.
 */
export function requireAdmin(req: Request, res: Response, next: NextFunction): void {
  if (req.caller.role !== 'admin') {
    res.status(403).json({ error: 'This route requires admin role' });
    return;
  }
  next();
}
