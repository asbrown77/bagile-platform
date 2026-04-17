export interface TrelloCardSummary {
  id: string;
  name: string;
  listName: string;
  dueDate?: string;
  isOverdue: boolean;
  url: string;
}

export interface ITrelloPort {
  getOpenCards(boardId: string): Promise<TrelloCardSummary[]>;
}
