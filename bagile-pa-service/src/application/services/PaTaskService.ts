import type { IPaTaskRepository, CreatePaTaskInput } from '../../domain/ports/IPaTaskRepository.js';
import type { PaTask } from '../../domain/entities/PaTask.js';

export class PaTaskService {
  constructor(private readonly repo: IPaTaskRepository) {}

  createTask(input: CreatePaTaskInput): Promise<PaTask> {
    return this.repo.create(input);
  }

  listOpen(tenantId: string, userId: string): Promise<PaTask[]> {
    return this.repo.listOpen(tenantId, userId);
  }

  async completeTask(id: string): Promise<PaTask> {
    const task = await this.repo.complete(id);
    if (task === null) {
      throw new Error('Task not found');
    }
    return task;
  }
}
