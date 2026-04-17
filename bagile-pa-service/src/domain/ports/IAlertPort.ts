export interface AlertPayload {
  automationName: string;
  tenantId: string;
  consecutiveFailures: number;
  lastError?: string;
  lastRunAt: Date;
}

export interface IAlertPort {
  sendAlert(payload: AlertPayload): Promise<void>;
}
