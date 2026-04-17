import type { IAlertPort, AlertPayload } from '../../domain/ports/IAlertPort.js';

export class StubAlertAdapter implements IAlertPort {
  readonly alerts: AlertPayload[] = [];

  async sendAlert(payload: AlertPayload): Promise<void> {
    this.alerts.push(payload);
    console.error(`[ALERT] ${payload.automationName}: ${payload.consecutiveFailures} consecutive failures`);
  }
}
