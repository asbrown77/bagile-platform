import { Pool } from 'pg';
import { createHttpServer } from './server.js';

const port = parseInt(process.env['PA_HTTP_PORT'] ?? '3001', 10);
const databaseUrl = process.env['DATABASE_URL'];
const apiKeysRaw = process.env['PA_API_KEYS'] ?? '';
const corsOrigins = (process.env['PA_CORS_ORIGINS'] ?? '').split(',').filter(Boolean);

if (!databaseUrl) {
  console.error('PA HTTP server: DATABASE_URL is required');
  process.exit(1);
}

if (!apiKeysRaw) {
  console.error('PA HTTP server: PA_API_KEYS is required');
  process.exit(1);
}

const pool = new Pool({ connectionString: databaseUrl });

const app = createHttpServer(pool, {
  apiKeysRaw,
  corsOrigins,
});

app.listen(port, () => {
  console.log(`PA HTTP server listening on port ${port}`);
});
