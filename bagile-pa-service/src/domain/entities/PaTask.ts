export type PaTaskStatus = 'open' | 'completed';

export interface PaTask {
  id: string;
  tenantId: string;
  userId: string;
  type: string;
  title: string;
  payload: Record<string, unknown>;
  status: PaTaskStatus;
  createdAt: Date;
  completedAt?: Date;
}
