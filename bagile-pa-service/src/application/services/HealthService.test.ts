import { describe, it, expect, vi } from 'vitest';
import { HealthService } from './HealthService.js';
import type { IHealthRepository, RecordHealthInput } from '../../domain/ports/IHealthRepository.js';
import type { IAlertPort } from '../../domain/ports/IAlertPort.js';
import type { HealthRecord } from '../../domain/entities/HealthRecord.js';

const BASE_RECORD: HealthRecord = {
  id: 'aaaaaaaa-0000-0000-0000-000000000001',
  automationName: 'morning_brief',
  tenantId: 'bagile',
  runAt: new Date('2026-04-16T09:00:00Z'),
  status: 'pass',
  durationMs: 1200,
  triggeredBy: 'schedule',
};

function makeRecord(overrides: Partial<HealthRecord> = {}): HealthRecord {
  return { ...BASE_RECORD, ...overrides };
}

function makeRepo(
  records: HealthRecord[] = [makeRecord()],
  recent: HealthRecord[] = []
): IHealthRepository {
  return {
    record: vi.fn().mockResolvedValue(records[0]),
    getRecentForAutomation: vi.fn().mockResolvedValue(recent),
    getLatestForAll: vi.fn().mockResolvedValue(records),
  };
}

function makeAlertPort(): IAlertPort {
  return { sendAlert: vi.fn().mockResolvedValue(undefined) };
}

const PASS_INPUT: RecordHealthInput = {
  automationName: 'morning_brief',
  tenantId: 'bagile',
  status: 'pass',
  durationMs: 1200,
  triggeredBy: 'schedule',
};

const FAIL_INPUT: RecordHealthInput = {
  ...PASS_INPUT,
  status: 'fail',
  errorMessage: 'Trello API timed out',
};

describe('HealthService', () => {
  it('records a passing run', async () => {
    const passRecord = makeRecord({ status: 'pass' });
    const repo = makeRepo([passRecord], [passRecord]);
    const alert = makeAlertPort();
    const service = new HealthService(repo, alert);

    const result = await service.recordRun(PASS_INPUT);

    expect(repo.record).toHaveBeenCalledWith(PASS_INPUT);
    expect(result.status).toBe('pass');
    expect(result.automationName).toBe('morning_brief');
  });

  it('records a failing run with error message', async () => {
    const failRecord = makeRecord({ status: 'fail', errorMessage: 'Trello API timed out' });
    const repo = makeRepo([failRecord], [failRecord]);
    const alert = makeAlertPort();
    const service = new HealthService(repo, alert);

    const result = await service.recordRun(FAIL_INPUT);

    expect(repo.record).toHaveBeenCalledWith(FAIL_INPUT);
    expect(result.status).toBe('fail');
    expect(result.errorMessage).toBe('Trello API timed out');
  });

  it('does NOT send alert when fewer than 3 consecutive failures', async () => {
    const twoFails = [
      makeRecord({ status: 'fail' }),
      makeRecord({ status: 'fail' }),
    ];
    const repo = makeRepo([makeRecord({ status: 'fail' })], twoFails);
    const alert = makeAlertPort();
    const service = new HealthService(repo, alert);

    await service.recordRun(FAIL_INPUT);

    expect(alert.sendAlert).not.toHaveBeenCalled();
  });

  it('sends alert on exactly 3 consecutive failures', async () => {
    const threeFails = [
      makeRecord({ status: 'fail', errorMessage: 'Trello API timed out' }),
      makeRecord({ status: 'fail' }),
      makeRecord({ status: 'fail' }),
    ];
    const repo = makeRepo([threeFails[0]], threeFails);
    const alert = makeAlertPort();
    const service = new HealthService(repo, alert);

    await service.recordRun(FAIL_INPUT);

    expect(alert.sendAlert).toHaveBeenCalledOnce();
    const payload = (alert.sendAlert as ReturnType<typeof vi.fn>).mock.calls[0][0];
    expect(payload.automationName).toBe('morning_brief');
    expect(payload.tenantId).toBe('bagile');
    expect(payload.consecutiveFailures).toBe(3);
    expect(payload.lastError).toBe('Trello API timed out');
  });

  it('sends alert again on 4th consecutive failure (alert fires every time once threshold met)', async () => {
    const fourFails = [
      makeRecord({ status: 'fail' }),
      makeRecord({ status: 'fail' }),
      makeRecord({ status: 'fail' }),
      makeRecord({ status: 'fail' }),
    ];
    // getRecentForAutomation returns last 3 — all fail
    const repo = makeRepo([fourFails[0]], fourFails.slice(0, 3));
    const alert = makeAlertPort();
    const service = new HealthService(repo, alert);

    await service.recordRun(FAIL_INPUT);

    expect(alert.sendAlert).toHaveBeenCalledOnce();
  });

  it('resets and does NOT alert when a pass follows 2 failures', async () => {
    // Recent history: 1 pass + 2 fails — not ALL fail
    const mixed = [
      makeRecord({ status: 'pass' }),
      makeRecord({ status: 'fail' }),
      makeRecord({ status: 'fail' }),
    ];
    const repo = makeRepo([makeRecord({ status: 'fail' })], mixed);
    const alert = makeAlertPort();
    const service = new HealthService(repo, alert);

    await service.recordRun(FAIL_INPUT);

    expect(alert.sendAlert).not.toHaveBeenCalled();
  });

  it('getStatus returns latest record per automation', async () => {
    const latestRecords = [
      makeRecord({ automationName: 'morning_brief', status: 'pass' }),
      makeRecord({ automationName: 'trello_sync', status: 'fail' }),
    ];
    const repo = makeRepo(latestRecords, []);
    const alert = makeAlertPort();
    const service = new HealthService(repo, alert);

    const result = await service.getStatus('bagile');

    expect(repo.getLatestForAll).toHaveBeenCalledWith('bagile');
    expect(result).toHaveLength(2);
    expect(result[0].automationName).toBe('morning_brief');
    expect(result[1].automationName).toBe('trello_sync');
  });
});
