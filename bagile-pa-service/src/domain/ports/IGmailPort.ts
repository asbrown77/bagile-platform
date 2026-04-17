export interface EmailSummary {
  id: string;
  from: string;
  subject: string;
  date: string;
  snippet: string;
}

export interface IGmailPort {
  getRecentEmails(options: { mailbox: string; days: number }): Promise<EmailSummary[]>;
}
