import { describe, it, expect, vi } from 'vitest';
import { UpdateTrelloCardUseCase } from './UpdateTrelloCardUseCase.js';
import type { ITrelloWritePort, TrelloCardUpdate, TrelloCardUpdateResult } from '../../../domain/ports/ITrelloWritePort.js';

function makeWritePort(result: TrelloCardUpdateResult): ITrelloWritePort {
  return { updateCard: vi.fn().mockResolvedValue(result) };
}

const BASE_UPDATE: TrelloCardUpdate = {
  cardId: 'card-abc',
  listId: 'list-123',
  comment: 'Done.',
  dueDate: '2026-04-20T09:00:00.000Z',
};

const BASE_RESULT: TrelloCardUpdateResult = {
  cardId: 'card-abc',
  moved: true,
  commented: true,
  dueUpdated: true,
};

describe('UpdateTrelloCardUseCase', () => {
  it('calls writePort.updateCard with the correct args', async () => {
    const port = makeWritePort(BASE_RESULT);
    const useCase = new UpdateTrelloCardUseCase(port);

    await useCase.execute(BASE_UPDATE);

    expect(port.updateCard).toHaveBeenCalledOnce();
    expect(port.updateCard).toHaveBeenCalledWith(BASE_UPDATE);
  });

  it('returns the result from writePort', async () => {
    const port = makeWritePort(BASE_RESULT);
    const useCase = new UpdateTrelloCardUseCase(port);

    const result = await useCase.execute(BASE_UPDATE);

    expect(result).toEqual(BASE_RESULT);
  });
});
