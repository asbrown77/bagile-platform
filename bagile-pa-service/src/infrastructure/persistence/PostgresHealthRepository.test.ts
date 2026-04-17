import { describe, it, expect, vi } from 'vitest';

vi.mock('pg', () => ({ Pool: vi.fn() }));

import { PostgresHealthRepository } from './PostgresHealthRepository.js';
import type { Pool } from 'pg';
import type { RecordHealthInput } from '../../domain/ports/IHealthRepository.js';

interface DbRow {
  id: string;
  automation_name: string;
  tenant_id: string;
  run_at: Date;
  status: string;
  duration_ms: number;
  error_message: string | null;
  triggered_by: string;
}

const RUN_AT = new Date('2026-04-16T09:00:00Z');

const SAMPLE_ROW: DbRow = {
  id: 'bbbbbbbb-0000-0000-0000-000000000001',
  automation_name: 'morning_brief',
  tenant_id: 'bagile',
  run_at: RUN_AT,
  status: 'pass',
  duration_ms: 1200,
  error_message: null,
  triggered_by: 'schedule',
};

const FAIL_ROW: DbRow = {
  ...SAMPLE_ROW,
  id: 'bbbbbbbb-0000-0000-0000-000000000002',
  status: 'fail',
  error_message: 'Trello timed out',
  triggered_by: 'schedule',
};

function makePool(rows: DbRow[] = []): Pool {
  return { query: vi.fn().mockResolvedValue({ rows }) } as unknown as Pool;
}

const PASS_INPUT: RecordHealthInput = {
  automationName: 'morning_brief',
  tenantId: 'bagile',
  status: 'pass',
  durationMs: 1200,
  triggeredBy: 'schedule',
};

describe('PostgresHealthRepository', () => {
  it('record: calls INSERT with correct params and returns mapped HealthRecord', async () => {
    const pool = makePool([SAMPLE_ROW]);
    const repo = new PostgresHealthRepository(pool);

    const result = await repo.record(PASS_INPUT);

    const querySpy = pool.query as ReturnType<typeof vi.fn>;
    expect(querySpy).toHaveBeenCalledOnce();
    const [sql, params] = querySpy.mock.calls[0] as [string, unknown[]];
    expect(sql).toContain('INSERT INTO bagile.health_records');
    expect(sql).toContain('RETURNING *');
    expect(params).toEqual(['morning_brief', 'bagile', 'pass', 1200, undefined, 'schedule']);

    expect(result.id).toBe(SAMPLE_ROW.id);
    expect(result.automationName).toBe('morning_brief');
    expect(result.tenantId).toBe('bagile');
    expect(result.runAt).toEqual(RUN_AT);
    expect(result.status).toBe('pass');
    expect(result.durationMs).toBe(1200);
    expect(result.errorMessage).toBeUndefined();
    expect(result.triggeredBy).toBe('schedule');
  });

  it('record: maps error_message correctly for failing runs', async () => {
    const pool = makePool([FAIL_ROW]);
    const repo = new PostgresHealthRepository(pool);

    const result = await repo.record({
      ...PASS_INPUT,
      status: 'fail',
      errorMessage: 'Trello timed out',
    });

    expect(result.status).toBe('fail');
    expect(result.errorMessage).toBe('Trello timed out');
  });

  it('getRecentForAutomation: calls SELECT with correct params and limit', async () => {
    const pool = makePool([SAMPLE_ROW, FAIL_ROW]);
    const repo = new PostgresHealthRepository(pool);

    const result = await repo.getRecentForAutomation('morning_brief', 'bagile', 3);

    const querySpy = pool.query as ReturnType<typeof vi.fn>;
    expect(querySpy).toHaveBeenCalledOnce();
    const [sql, params] = querySpy.mock.calls[0] as [string, unknown[]];
    expect(sql).toContain('bagile.health_records');
    expect(sql).toContain('ORDER BY run_at DESC');
    expect(sql).toContain('LIMIT');
    expect(params).toEqual(['morning_brief', 'bagile', 3]);

    expect(result).toHaveLength(2);
    expect(result[0].automationName).toBe('morning_brief');
  });

  it('getLatestForAll: calls DISTINCT ON query with tenant param', async () => {
    const trelloRow: DbRow = {
      ...SAMPLE_ROW,
      automation_name: 'trello_sync',
      status: 'fail',
      error_message: 'timeout',
    };
    const pool = makePool([SAMPLE_ROW, trelloRow]);
    const repo = new PostgresHealthRepository(pool);

    const result = await repo.getLatestForAll('bagile');

    const querySpy = pool.query as ReturnType<typeof vi.fn>;
    expect(querySpy).toHaveBeenCalledOnce();
    const [sql, params] = querySpy.mock.calls[0] as [string, unknown[]];
    expect(sql).toContain('DISTINCT ON');
    expect(sql).toContain('automation_name');
    expect(params).toEqual(['bagile']);

    expect(result).toHaveLength(2);
    expect(result[1].automationName).toBe('trello_sync');
    expect(result[1].errorMessage).toBe('timeout');
  });
});
