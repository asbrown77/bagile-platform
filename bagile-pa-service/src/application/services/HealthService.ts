import type { IHealthRepository, RecordHealthInput } from '../../domain/ports/IHealthRepository.js';
import type { IAlertPort } from '../../domain/ports/IAlertPort.js';
import type { HealthRecord } from '../../domain/entities/HealthRecord.js';

const ALERT_THRESHOLD = 3;

export class HealthService {
  constructor(
    private readonly repo: IHealthRepository,
    private readonly alertPort: IAlertPort
  ) {}

  async recordRun(input: RecordHealthInput): Promise<HealthRecord> {
    const record = await this.repo.record(input);
    const recent = await this.repo.getRecentForAutomation(
      input.automationName,
      input.tenantId,
      ALERT_THRESHOLD
    );
    const allFail = recent.length >= ALERT_THRESHOLD && recent.every(r => r.status === 'fail');
    if (allFail) {
      await this.alertPort.sendAlert({
        automationName: input.automationName,
        tenantId: input.tenantId,
        consecutiveFailures: recent.length,
        lastError: input.errorMessage,
        lastRunAt: record.runAt,
      });
    }
    return record;
  }

  getStatus(tenantId: string): Promise<HealthRecord[]> {
    return this.repo.getLatestForAll(tenantId);
  }
}
