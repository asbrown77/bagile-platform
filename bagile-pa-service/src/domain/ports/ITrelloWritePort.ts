export interface TrelloCardUpdate {
  cardId: string;
  listId?: string;
  comment?: string;
  dueDate?: string | null;  // ISO string or null to clear
}

export interface TrelloCardUpdateResult {
  cardId: string;
  moved: boolean;
  commented: boolean;
  dueUpdated: boolean;
}

export interface ITrelloWritePort {
  updateCard(update: TrelloCardUpdate): Promise<TrelloCardUpdateResult>;
}
