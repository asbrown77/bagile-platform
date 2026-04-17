import type { ITrelloPort, TrelloCardSummary } from "../../../domain/ports/ITrelloPort.js";
import { fetchJson } from "../../http/fetchJson.js";

interface TrelloCard {
  id: string;
  name: string;
  idList: string;
  due: string | null;
  url: string;
}

interface TrelloList {
  id: string;
  name: string;
}

const TRELLO_BASE = "https://api.trello.com";

export class TrelloAdapter implements ITrelloPort {
  private readonly key: string;
  private readonly token: string;

  constructor(key: string, token: string) {
    this.key = key;
    this.token = token;
  }

  async getOpenCards(boardId: string): Promise<TrelloCardSummary[]> {
    const authParams = `key=${this.key}&token=${this.token}`;

    const [cards, lists] = await Promise.all([
      fetchJson<TrelloCard[]>(
        `${TRELLO_BASE}/1/boards/${boardId}/cards?filter=open&fields=name,idList,due,url&${authParams}`
      ),
      fetchJson<TrelloList[]>(
        `${TRELLO_BASE}/1/boards/${boardId}/lists?filter=open&fields=name&${authParams}`
      ),
    ]);

    const listMap = new Map(lists.map((l) => [l.id, l.name]));

    return cards.map((card) => ({
      id: card.id,
      name: card.name,
      listName: listMap.get(card.idList) ?? "Unknown",
      dueDate: card.due ?? undefined,
      isOverdue: card.due != null && new Date(card.due) < new Date(),
      url: card.url,
    }));
  }
}
