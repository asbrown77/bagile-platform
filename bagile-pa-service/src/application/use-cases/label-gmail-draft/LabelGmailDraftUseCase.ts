import type { IN8nPort } from '../../../domain/ports/IN8nPort.js';

export interface LabelGmailDraftInput {
  messageId: string;
}

export interface LabelGmailDraftResult {
  messageId: string;
  labelled: boolean;
}

export class LabelGmailDraftUseCase {
  constructor(private readonly n8nPort: IN8nPort) {}

  async execute(input: LabelGmailDraftInput): Promise<LabelGmailDraftResult> {
    await this.n8nPort.labelGmailDraftAsPam(input.messageId);
    return { messageId: input.messageId, labelled: true };
  }
}
