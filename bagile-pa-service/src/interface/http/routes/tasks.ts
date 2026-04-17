import { Router } from 'express';
import { Pool } from 'pg';
import { PostgresPaTaskRepository } from '../../../infrastructure/persistence/PostgresPaTaskRepository.js';
import { PaTaskService } from '../../../application/services/PaTaskService.js';

const UUID_RE = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function createTasksRouter(pool: Pool): Router {
  const router = Router();
  const service = new PaTaskService(new PostgresPaTaskRepository(pool));

  // GET /tasks — list open tasks for the authenticated caller
  router.get('/', async (req, res) => {
    const { userId, tenantId } = req.caller;
    const tasks = await service.listOpen(tenantId, userId);
    res.json(tasks);
  });

  // PATCH /tasks/:id — mark a task complete
  router.patch('/:id', async (req, res) => {
    const { id } = req.params;
    if (!UUID_RE.test(id)) {
      res.status(400).json({ error: 'Invalid task ID format' });
      return;
    }
    try {
      const task = await service.completeTask(id);
      res.json(task);
    } catch (err) {
      res.status(404).json({ error: (err as Error).message });
    }
  });

  return router;
}
