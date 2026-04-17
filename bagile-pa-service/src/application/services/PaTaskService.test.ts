import { describe, it, expect, vi } from 'vitest';
import { PaTaskService } from './PaTaskService.js';
import type { IPaTaskRepository, CreatePaTaskInput } from '../../domain/ports/IPaTaskRepository.js';
import type { PaTask } from '../../domain/entities/PaTask.js';

const SAMPLE_TASK: PaTask = {
  id: 'a1b2c3d4-0000-0000-0000-000000000001',
  tenantId: 'bagile',
  userId: 'alex',
  type: 'follow_up',
  title: 'Chase Acme re: PSM booking',
  payload: { trelloCardId: 'c1' },
  status: 'open',
  createdAt: new Date('2026-04-16T09:00:00Z'),
};

const COMPLETED_TASK: PaTask = {
  ...SAMPLE_TASK,
  status: 'completed',
  completedAt: new Date('2026-04-16T10:00:00Z'),
};

function makeRepo(overrides: Partial<IPaTaskRepository> = {}): IPaTaskRepository {
  return {
    create: vi.fn().mockResolvedValue(SAMPLE_TASK),
    listOpen: vi.fn().mockResolvedValue([SAMPLE_TASK]),
    complete: vi.fn().mockResolvedValue(COMPLETED_TASK),
    ...overrides,
  };
}

describe('PaTaskService', () => {
  it('createTask: delegates to repo.create and returns the task', async () => {
    const repo = makeRepo();
    const service = new PaTaskService(repo);

    const input: CreatePaTaskInput = {
      tenantId: 'bagile',
      userId: 'alex',
      type: 'follow_up',
      title: 'Chase Acme re: PSM booking',
      payload: { trelloCardId: 'c1' },
    };

    const result = await service.createTask(input);

    expect(repo.create).toHaveBeenCalledWith(input);
    expect(result).toEqual(SAMPLE_TASK);
  });

  it('listOpen: returns tasks from repo', async () => {
    const repo = makeRepo();
    const service = new PaTaskService(repo);

    const result = await service.listOpen('bagile', 'alex');

    expect(repo.listOpen).toHaveBeenCalledWith('bagile', 'alex');
    expect(result).toEqual([SAMPLE_TASK]);
  });

  it('completeTask: returns completed task when found', async () => {
    const repo = makeRepo();
    const service = new PaTaskService(repo);

    const result = await service.completeTask(SAMPLE_TASK.id);

    expect(repo.complete).toHaveBeenCalledWith(SAMPLE_TASK.id);
    expect(result).toEqual(COMPLETED_TASK);
    expect(result.status).toBe('completed');
  });

  it('completeTask: throws "Task not found" when repo returns null', async () => {
    const repo = makeRepo({ complete: vi.fn().mockResolvedValue(null) });
    const service = new PaTaskService(repo);

    await expect(service.completeTask('nonexistent-id')).rejects.toThrow('Task not found');
  });
});
