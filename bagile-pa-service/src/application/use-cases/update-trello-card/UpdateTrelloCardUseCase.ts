import type {
  ITrelloWritePort,
  TrelloCardUpdate,
  TrelloCardUpdateResult,
} from '../../../domain/ports/ITrelloWritePort.js';

export class UpdateTrelloCardUseCase {
  constructor(private readonly writePort: ITrelloWritePort) {}

  async execute(update: TrelloCardUpdate): Promise<TrelloCardUpdateResult> {
    return this.writePort.updateCard(update);
  }
}
