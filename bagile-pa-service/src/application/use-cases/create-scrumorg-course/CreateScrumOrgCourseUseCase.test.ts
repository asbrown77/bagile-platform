import { describe, it, expect, vi } from 'vitest';
import { CreateScrumOrgCourseUseCase } from './CreateScrumOrgCourseUseCase.js';
import type {
  IPlaywrightRunnerPort,
  PlaywrightRunOptions,
  PlaywrightRunResult,
} from '../../../domain/ports/IPlaywrightRunnerPort.js';

function makeRunner(result: PlaywrightRunResult): IPlaywrightRunnerPort {
  return { run: vi.fn().mockResolvedValue(result) };
}

const BASE_INPUT = {
  courseType: 'PSM',
  trainerName: 'Chris Bexon',
  startDate: '2026-06-10',
  endDate: '2026-06-11',
  registrationUrl: 'https://www.bagile.co.uk/product/psm-100626-cb/',
  tenantId: 'bagile',
};

describe('CreateScrumOrgCourseUseCase', () => {
  it('returns success with courseUrl when runner succeeds', async () => {
    const runner = makeRunner({
      success: true,
      output: { courseUrl: 'https://www.scrum.org/courses/professional-scrum-master/12345' },
      durationMs: 4200,
    });
    const useCase = new CreateScrumOrgCourseUseCase(runner);

    const result = await useCase.execute(BASE_INPUT);

    expect(result.success).toBe(true);
    expect(result.courseUrl).toBe('https://www.scrum.org/courses/professional-scrum-master/12345');
    expect(result.durationMs).toBe(4200);
    expect(result.errorMessage).toBeUndefined();
  });

  it('returns failure without throwing when runner fails', async () => {
    const runner = makeRunner({
      success: false,
      errorMessage: 'Login failed — invalid credentials',
      durationMs: 900,
    });
    const useCase = new CreateScrumOrgCourseUseCase(runner);

    const result = await useCase.execute(BASE_INPUT);

    expect(result.success).toBe(false);
    expect(result.errorMessage).toBe('Login failed — invalid credentials');
    expect(result.durationMs).toBe(900);
    expect(result.courseUrl).toBeUndefined();
  });

  it('passes scriptName scrumorg-create-course to runner', async () => {
    const runner = makeRunner({
      success: true,
      output: { courseUrl: 'https://www.scrum.org/courses/12345' },
      durationMs: 100,
    });
    const useCase = new CreateScrumOrgCourseUseCase(runner);

    await useCase.execute(BASE_INPUT);

    const callArg = (runner.run as ReturnType<typeof vi.fn>).mock.calls[0][0] as PlaywrightRunOptions;
    expect(callArg.scriptName).toBe('scrumorg-create-course');
    expect(callArg.tenantId).toBe('bagile');
  });

  it('passes correct input fields to runner', async () => {
    const runner = makeRunner({
      success: true,
      output: { courseUrl: 'https://www.scrum.org/courses/12345' },
      durationMs: 100,
    });
    const useCase = new CreateScrumOrgCourseUseCase(runner);

    await useCase.execute(BASE_INPUT);

    const callArg = (runner.run as ReturnType<typeof vi.fn>).mock.calls[0][0] as PlaywrightRunOptions;
    expect(callArg.input['courseType']).toBe('PSM');
    expect(callArg.input['trainerName']).toBe('Chris Bexon');
    expect(callArg.input['startDate']).toBe('2026-06-10');
    expect(callArg.input['endDate']).toBe('2026-06-11');
    expect(callArg.input['registrationUrl']).toBe('https://www.bagile.co.uk/product/psm-100626-cb/');
  });
});
