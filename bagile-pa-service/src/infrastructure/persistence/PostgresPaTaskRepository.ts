import { Pool } from 'pg';
import type { IPaTaskRepository, CreatePaTaskInput } from '../../domain/ports/IPaTaskRepository.js';
import type { PaTask } from '../../domain/entities/PaTask.js';

interface DbRow {
  id: string;
  tenant_id: string;
  user_id: string;
  type: string;
  title: string;
  payload: Record<string, unknown>;
  status: string;
  created_at: Date;
  completed_at: Date | null;
}

function mapRow(row: DbRow): PaTask {
  return {
    id: row.id,
    tenantId: row.tenant_id,
    userId: row.user_id,
    type: row.type,
    title: row.title,
    payload: row.payload,
    status: row.status as PaTask['status'],
    createdAt: row.created_at,
    completedAt: row.completed_at ?? undefined,
  };
}

export class PostgresPaTaskRepository implements IPaTaskRepository {
  constructor(private readonly pool: Pool) {}

  async create(input: CreatePaTaskInput): Promise<PaTask> {
    const { rows } = await this.pool.query<DbRow>(
      `INSERT INTO bagile.pa_tasks (tenant_id, user_id, type, title, payload)
       VALUES ($1, $2, $3, $4, $5)
       RETURNING *`,
      [input.tenantId, input.userId, input.type, input.title, JSON.stringify(input.payload ?? {})]
    );
    return mapRow(rows[0]);
  }

  async listOpen(tenantId: string, userId: string): Promise<PaTask[]> {
    const { rows } = await this.pool.query<DbRow>(
      `SELECT * FROM bagile.pa_tasks
       WHERE tenant_id = $1 AND user_id = $2 AND status = 'open'
       ORDER BY created_at DESC`,
      [tenantId, userId]
    );
    return rows.map(mapRow);
  }

  async complete(id: string): Promise<PaTask | null> {
    const { rows } = await this.pool.query<DbRow>(
      `UPDATE bagile.pa_tasks
       SET status = 'completed', completed_at = now()
       WHERE id = $1 AND status = 'open'
       RETURNING *`,
      [id]
    );
    return rows.length > 0 ? mapRow(rows[0]) : null;
  }
}
