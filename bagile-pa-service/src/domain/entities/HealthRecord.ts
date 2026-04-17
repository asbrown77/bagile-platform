export type HealthStatus = 'pass' | 'fail' | 'degraded';
export type HealthTrigger = 'schedule' | 'manual' | 'webhook';

export interface HealthRecord {
  id: string;
  automationName: string;
  tenantId: string;
  runAt: Date;
  status: HealthStatus;
  durationMs: number;
  errorMessage?: string;
  triggeredBy: HealthTrigger;
}
