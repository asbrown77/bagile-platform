import type {
  ITrelloWritePort,
  TrelloCardUpdate,
  TrelloCardUpdateResult,
} from '../../../domain/ports/ITrelloWritePort.js';
import { fetchJson } from '../../http/fetchJson.js';

const TRELLO_BASE = 'https://api.trello.com/1';

export class TrelloWriteAdapter implements ITrelloWritePort {
  constructor(
    private readonly apiKey: string,
    private readonly token: string
  ) {}

  async updateCard(update: TrelloCardUpdate): Promise<TrelloCardUpdateResult> {
    const auth = `key=${this.apiKey}&token=${this.token}`;
    let moved = false;
    let commented = false;
    let dueUpdated = false;

    if (update.listId !== undefined) {
      await fetchJson(`${TRELLO_BASE}/cards/${update.cardId}?${auth}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ idList: update.listId }),
      });
      moved = true;
    }

    if (update.comment !== undefined) {
      await fetchJson(`${TRELLO_BASE}/cards/${update.cardId}/actions/comments?${auth}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ text: update.comment }),
      });
      commented = true;
    }

    if ('dueDate' in update) {
      await fetchJson(`${TRELLO_BASE}/cards/${update.cardId}?${auth}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ due: update.dueDate ?? null }),
      });
      dueUpdated = true;
    }

    return { cardId: update.cardId, moved, commented, dueUpdated };
  }
}
