import express from 'express';
import cors from 'cors';
import rateLimit from 'express-rate-limit';
import { Pool } from 'pg';
import { createTasksRouter } from './routes/tasks.js';
import { createCredentialsRouter } from './routes/credentials.js';
import { apiKeyAuth, parseApiKeys } from './middleware/apiKeyAuth.js';

const DEFAULT_RATE_LIMIT = 60; // requests per minute per IP

export interface HttpServerOptions {
  /** Comma-separated API key entries: "key:name:userId:tenantId:role" */
  apiKeysRaw: string;
  /** Allowed CORS origins, e.g. "https://portal.bagile.co.uk" */
  corsOrigins?: string[];
  /** Requests per minute per IP (default: 60) */
  rateLimitPerMinute?: number;
}

export function createHttpServer(pool: Pool, opts: HttpServerOptions): express.Application {
  const app = express();

  // CORS — portal and trusted origins only
  const allowedOrigins = opts.corsOrigins ?? [];
  app.use(cors({
    origin: (origin, callback) => {
      // Allow requests with no Origin (server-to-server, curl) or matching allowed origins
      if (!origin || allowedOrigins.length === 0 || allowedOrigins.includes(origin)) {
        callback(null, true);
      } else {
        callback(new Error('Not allowed by CORS'));
      }
    },
    methods: ['GET', 'PATCH', 'POST'],
    allowedHeaders: ['Authorization', 'Content-Type'],
  }));

  // Rate limiting
  app.use(rateLimit({
    windowMs: 60 * 1000,
    max: opts.rateLimitPerMinute ?? DEFAULT_RATE_LIMIT,
    standardHeaders: true,
    legacyHeaders: false,
    message: { error: 'Too many requests — slow down' },
  }));

  app.use(express.json());

  // Public health check — no auth required
  app.get('/health', (_req, res) => res.json({ status: 'ok' }));

  // All other routes require a valid API key
  const keys = parseApiKeys(opts.apiKeysRaw);
  app.use(apiKeyAuth(keys));

  // STANDARD routes — any valid key (admin or reader)
  app.use('/tasks', createTasksRouter(pool));

  // ADMIN routes — admin key required (enforced inside the router)
  app.use('/credentials', createCredentialsRouter(pool));

  return app;
}
