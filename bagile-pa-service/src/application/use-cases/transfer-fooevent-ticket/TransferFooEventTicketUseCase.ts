import type { IPlaywrightRunnerPort } from '../../../domain/ports/IPlaywrightRunnerPort.js';
import type { CredentialResolver } from '../../../domain/ports/ICredentialStore.js';

export interface TransferFooEventTicketInput {
  oldTicketPostId: number;
  newProductId: number;
  attendeeFirstName: string;
  attendeeLastName: string;
  attendeeEmail: string;
  attendeeCompany?: string;
  fromCourseCode: string;
  toCourseCode: string;
  tenantId: string;
  credentialResolver?: CredentialResolver;
}

export interface TransferFooEventTicketResult {
  success: boolean;
  newTicketPostId?: number;
  errorMessage?: string;
  durationMs: number;
}

export class TransferFooEventTicketUseCase {
  constructor(private readonly runner: IPlaywrightRunnerPort) {}

  async execute(input: TransferFooEventTicketInput): Promise<TransferFooEventTicketResult> {
    const designation = `Transferred from ${input.fromCourseCode} to ${input.toCourseCode}`;

    const runResult = await this.runner.run({
      scriptName: 'fooevent-transfer',
      tenantId: input.tenantId,
      credentialResolver: input.credentialResolver,
      input: {
        oldTicketPostId: input.oldTicketPostId,
        newProductId: input.newProductId,
        attendeeFirstName: input.attendeeFirstName,
        attendeeLastName: input.attendeeLastName,
        attendeeEmail: input.attendeeEmail,
        attendeeCompany: input.attendeeCompany ?? '',
        designation,
      },
    });

    if (runResult.success) {
      return {
        success: true,
        newTicketPostId: runResult.output?.['newTicketPostId'] as number | undefined,
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
