import { describe, it, expect, vi } from 'vitest';
import { LabelGmailDraftUseCase } from './LabelGmailDraftUseCase.js';
import type { IN8nPort } from '../../../domain/ports/IN8nPort.js';

function makeN8nPort(): IN8nPort {
  return { labelGmailDraftAsPam: vi.fn().mockResolvedValue(undefined) };
}

describe('LabelGmailDraftUseCase', () => {
  it('calls n8nPort.labelGmailDraftAsPam with the correct messageId', async () => {
    const port = makeN8nPort();
    const useCase = new LabelGmailDraftUseCase(port);

    await useCase.execute({ messageId: 'msg-xyz-001' });

    expect(port.labelGmailDraftAsPam).toHaveBeenCalledOnce();
    expect(port.labelGmailDraftAsPam).toHaveBeenCalledWith('msg-xyz-001');
  });

  it('returns { labelled: true } with the messageId', async () => {
    const port = makeN8nPort();
    const useCase = new LabelGmailDraftUseCase(port);

    const result = await useCase.execute({ messageId: 'msg-xyz-001' });

    expect(result).toEqual({ messageId: 'msg-xyz-001', labelled: true });
  });
});
