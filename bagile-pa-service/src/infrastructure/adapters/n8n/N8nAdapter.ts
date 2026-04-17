import type { IN8nPort } from '../../../domain/ports/IN8nPort.js';
import { fetchJson } from '../../http/fetchJson.js';

export class N8nAdapter implements IN8nPort {
  constructor(private readonly n8nBaseUrl: string) {}

  async labelGmailDraftAsPam(messageId: string): Promise<void> {
    await fetchJson(`${this.n8nBaseUrl}/webhook/label-draft-pam`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ messageId }),
    });
  }
}
