import type { IPlaywrightRunnerPort } from '../../../domain/ports/IPlaywrightRunnerPort.js';
import type { CredentialResolver } from '../../../domain/ports/ICredentialStore.js';

export interface CreateScrumOrgCourseInput {
  courseType: string;
  trainerName: string;
  startDate: string;       // YYYY-MM-DD
  endDate: string;         // YYYY-MM-DD
  registrationUrl: string;
  tenantId: string;
  credentialResolver?: CredentialResolver;
}

export interface CreateScrumOrgCourseResult {
  success: boolean;
  courseUrl?: string;
  errorMessage?: string;
  durationMs: number;
}

export class CreateScrumOrgCourseUseCase {
  constructor(private readonly runner: IPlaywrightRunnerPort) {}

  async execute(input: CreateScrumOrgCourseInput): Promise<CreateScrumOrgCourseResult> {
    const runResult = await this.runner.run({
      scriptName: 'scrumorg-create-course',
      tenantId: input.tenantId,
      credentialResolver: input.credentialResolver,
      input: {
        courseType: input.courseType,
        trainerName: input.trainerName,
        startDate: input.startDate,
        endDate: input.endDate,
        registrationUrl: input.registrationUrl,
      },
    });

    if (runResult.success) {
      return {
        success: true,
        courseUrl: runResult.output?.['courseUrl'] as string | undefined,
        durationMs: runResult.durationMs,
      };
    }

    return {
      success: false,
      errorMessage: runResult.errorMessage,
      durationMs: runResult.durationMs,
    };
  }
}
