import { Pool } from 'pg';
import type { IHealthRepository, RecordHealthInput } from '../../domain/ports/IHealthRepository.js';
import type { HealthRecord } from '../../domain/entities/HealthRecord.js';

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

function mapRow(row: DbRow): HealthRecord {
  return {
    id: row.id,
    automationName: row.automation_name,
    tenantId: row.tenant_id,
    runAt: row.run_at,
    status: row.status as HealthRecord['status'],
    durationMs: row.duration_ms,
    errorMessage: row.error_message ?? undefined,
    triggeredBy: row.triggered_by as HealthRecord['triggeredBy'],
  };
}

export class PostgresHealthRepository implements IHealthRepository {
  constructor(private readonly pool: Pool) {}

  async record(input: RecordHealthInput): Promise<HealthRecord> {
    const { rows } = await this.pool.query<DbRow>(
      `INSERT INTO bagile.health_records
         (automation_name, tenant_id, status, duration_ms, error_message, triggered_by)
       VALUES ($1, $2, $3, $4, $5, $6)
       RETURNING *`,
      [
        input.automationName,
        input.tenantId,
        input.status,
        input.durationMs,
        input.errorMessage,
        input.triggeredBy,
      ]
    );
    return mapRow(rows[0]);
  }

  async getRecentForAutomation(
    automationName: string,
    tenantId: string,
    limit: number
  ): Promise<HealthRecord[]> {
    const { rows } = await this.pool.query<DbRow>(
      `SELECT * FROM bagile.health_records
       WHERE automation_name = $1 AND tenant_id = $2
       ORDER BY run_at DESC
       LIMIT $3`,
      [automationName, tenantId, limit]
    );
    return rows.map(mapRow);
  }

  async getLatestForAll(tenantId: string): Promise<HealthRecord[]> {
    const { rows } = await this.pool.query<DbRow>(
      `SELECT DISTINCT ON (automation_name) *
       FROM bagile.health_records
       WHERE tenant_id = $1
       ORDER BY automation_name, run_at DESC`,
      [tenantId]
    );
    return rows.map(mapRow);
  }
}
