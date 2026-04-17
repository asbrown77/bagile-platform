import type { EmailSummary, IGmailPort } from "../../../domain/ports/IGmailPort.js";

export class GmailStubAdapter implements IGmailPort {
  async getRecentEmails(_options: { mailbox: string; days: number }): Promise<EmailSummary[]> {
    return [];
  }
}
