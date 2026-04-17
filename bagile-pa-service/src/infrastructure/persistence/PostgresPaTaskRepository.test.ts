import { describe, it, expect, vi } from 'vitest';

vi.mock('pg', () => ({ Pool: vi.fn() }));

import { PostgresPaTaskRepository } from './PostgresPaTaskRepository.js';
import type { Pool } from 'pg';

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

const CREATED_AT = new Date('2026-04-16T09:00:00Z');

const SAMPLE_ROW: DbRow = {
  id: 'a1b2c3d4-0000-0000-0000-000000000001',
  tenant_id: 'bagile',
  user_id: 'alex',
  type: 'follow_up',
  title: 'Chase Acme re: PSM booking',
  payload: { trelloCardId: 'c1' },
  status: 'open',
  created_at: CREATED_AT,
  completed_at: null,
};

const COMPLETED_ROW: DbRow = {
  ...SAMPLE_ROW,
  status: 'completed',
  completed_at: new Date('2026-04-16T10:00:00Z'),
};

function makePool(rows: DbRow[] = []): Pool {
  return { query: vi.fn().mockResolvedValue({ rows }) } as unknown as Pool;
}

describe('PostgresPaTaskRepository', () => {
  it('create: calls INSERT with correct params and returns mapped PaTask', async () => {
    const pool = makePool([SAMPLE_ROW]);
    const repo = new PostgresPaTaskRepository(pool);

    const result = await repo.create({
      tenantId: 'bagile',
      userId: 'alex',
      type: 'follow_up',
      title: 'Chase Acme re: PSM booking',
      payload: { trelloCardId: 'c1' },
    });

    const querySpy = pool.query as ReturnType<typeof vi.fn>;
    expect(querySpy).toHaveBeenCalledOnce();
    const [sql, params] = querySpy.mock.calls[0] as [string, unknown[]];
    expect(sql).toContain('INSERT INTO bagile.pa_tasks');
    expect(sql).toContain('RETURNING *');
    expect(params).toEqual(['bagile', 'alex', 'follow_up', 'Chase Acme re: PSM booking', JSON.stringify({ trelloCardId: 'c1' })]);

    expect(result.id).toBe(SAMPLE_ROW.id);
    expect(result.tenantId).toBe('bagile');
    expect(result.userId).toBe('alex');
    expect(result.type).toBe('follow_up');
    expect(result.title).toBe('Chase Acme re: PSM booking');
    expect(result.payload).toEqual({ trelloCardId: 'c1' });
    expect(result.status).toBe('open');
    expect(result.createdAt).toEqual(CREATED_AT);
    expect(result.completedAt).toBeUndefined();
  });

  it('create: uses empty object payload when payload is omitted', async () => {
    const pool = makePool([{ ...SAMPLE_ROW, payload: {} }]);
    const repo = new PostgresPaTaskRepository(pool);

    await repo.create({ tenantId: 'bagile', userId: 'alex', type: 'test', title: 'Test' });

    const querySpy = pool.query as ReturnType<typeof vi.fn>;
    const [, params] = querySpy.mock.calls[0] as [string, unknown[]];
    expect(params[4]).toBe('{}');
  });

  it('listOpen: calls SELECT with correct params and returns all rows mapped', async () => {
    const pool = makePool([SAMPLE_ROW]);
    const repo = new PostgresPaTaskRepository(pool);

    const result = await repo.listOpen('bagile', 'alex');

    const querySpy = pool.query as ReturnType<typeof vi.fn>;
    expect(querySpy).toHaveBeenCalledOnce();
    const [sql, params] = querySpy.mock.calls[0] as [string, unknown[]];
    expect(sql).toContain('SELECT * FROM bagile.pa_tasks');
    expect(sql).toContain("status = 'open'");
    expect(params).toEqual(['bagile', 'alex']);

    expect(result).toHaveLength(1);
    expect(result[0].id).toBe(SAMPLE_ROW.id);
    expect(result[0].status).toBe('open');
  });

  it('complete: returns mapped task when row found', async () => {
    const pool = makePool([COMPLETED_ROW]);
    const repo = new PostgresPaTaskRepository(pool);

    const result = await repo.complete(SAMPLE_ROW.id);

    const querySpy = pool.query as ReturnType<typeof vi.fn>;
    const [sql, params] = querySpy.mock.calls[0] as [string, unknown[]];
    expect(sql).toContain('UPDATE bagile.pa_tasks');
    expect(sql).toContain("status = 'completed'");
    expect(params).toEqual([SAMPLE_ROW.id]);

    expect(result).not.toBeNull();
    expect(result!.status).toBe('completed');
    expect(result!.completedAt).toEqual(COMPLETED_ROW.completed_at);
  });

  it('complete: returns null when no row found (already completed or missing)', async () => {
    const pool = makePool([]);
    const repo = new PostgresPaTaskRepository(pool);

    const result = await repo.complete('nonexistent-id');

    expect(result).toBeNull();
  });
});
