import type { PaTask } from '../entities/PaTask.js';

export interface CreatePaTaskInput {
  tenantId: string;
  userId: string;
  type: string;
  title: string;
  payload?: Record<string, unknown>;
}

export interface IPaTaskRepository {
  create(input: CreatePaTaskInput): Promise<PaTask>;
  listOpen(tenantId: string, userId: string): Promise<PaTask[]>;
  complete(id: string): Promise<PaTask | null>;
}
