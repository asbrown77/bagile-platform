import type { HealthRecord } from '../entities/HealthRecord.js';

export interface RecordHealthInput {
  automationName: string;
  tenantId: string;
  status: HealthRecord['status'];
  durationMs: number;
  errorMessage?: string;
  triggeredBy: HealthRecord['triggeredBy'];
}

export interface IHealthRepository {
  record(input: RecordHealthInput): Promise<HealthRecord>;
  getRecentForAutomation(automationName: string, tenantId: string, limit: number): Promise<HealthRecord[]>;
  getLatestForAll(tenantId: string): Promise<HealthRecord[]>;
}
