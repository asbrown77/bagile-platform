import { describe, it, expect, vi } from 'vitest';
import { TransferFooEventTicketUseCase } from './TransferFooEventTicketUseCase.js';
import type {
  IPlaywrightRunnerPort,
  PlaywrightRunOptions,
  PlaywrightRunResult,
} from '../../../domain/ports/IPlaywrightRunnerPort.js';

function makeRunner(result: PlaywrightRunResult): IPlaywrightRunnerPort {
  return { run: vi.fn().mockResolvedValue(result) };
}

const BASE_INPUT = {
  oldTicketPostId: 101,
  newProductId: 202,
  attendeeFirstName: 'Jane',
  attendeeLastName: 'Smith',
  attendeeEmail: 'jane@example.com',
  attendeeCompany: 'Acme Corp',
  fromCourseCode: 'PSPO-260326-CB',
  toCourseCode: 'PSPO-150526-AB',
  tenantId: 'bagile',
};

describe('TransferFooEventTicketUseCase', () => {
  it('returns success result when runner succeeds', async () => {
    const runner = makeRunner({
      success: true,
      output: { newTicketPostId: 42 },
      durationMs: 1500,
    });
    const useCase = new TransferFooEventTicketUseCase(runner);

    const result = await useCase.execute(BASE_INPUT);

    expect(result.success).toBe(true);
    expect(result.newTicketPostId).toBe(42);
    expect(result.durationMs).toBe(1500);
    expect(result.errorMessage).toBeUndefined();
  });

  it('returns failure result when runner fails, does not throw', async () => {
    const runner = makeRunner({
      success: false,
      errorMessage: 'Login failed',
      durationMs: 800,
    });
    const useCase = new TransferFooEventTicketUseCase(runner);

    const result = await useCase.execute(BASE_INPUT);

    expect(result.success).toBe(false);
    expect(result.errorMessage).toBe('Login failed');
    expect(result.durationMs).toBe(800);
    expect(result.newTicketPostId).toBeUndefined();
  });

  it('passes correct script name and input to runner', async () => {
    const runner = makeRunner({ success: true, output: { newTicketPostId: 7 }, durationMs: 100 });
    const useCase = new TransferFooEventTicketUseCase(runner);

    await useCase.execute(BASE_INPUT);

    const callArg = (runner.run as ReturnType<typeof vi.fn>).mock
      .calls[0][0] as PlaywrightRunOptions;
    expect(callArg.scriptName).toBe('fooevent-transfer');
    expect(callArg.tenantId).toBe('bagile');
  });

  it('constructs designation string from course codes', async () => {
    const runner = makeRunner({ success: true, output: { newTicketPostId: 7 }, durationMs: 100 });
    const useCase = new TransferFooEventTicketUseCase(runner);

    await useCase.execute(BASE_INPUT);

    const callArg = (runner.run as ReturnType<typeof vi.fn>).mock
      .calls[0][0] as PlaywrightRunOptions;
    const capturedInput = callArg.input;
    expect(capturedInput['designation']).toBe(
      'Transferred from PSPO-260326-CB to PSPO-150526-AB'
    );
  });
});
