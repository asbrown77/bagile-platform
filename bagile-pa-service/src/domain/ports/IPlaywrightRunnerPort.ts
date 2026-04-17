import type { CredentialResolver } from './ICredentialStore.js';

export interface PlaywrightRunOptions {
  scriptName: string;
  tenantId: string;
  input: Record<string, unknown>;
  headless?: boolean;
  /** If provided, consulted before falling back to env vars for personal credentials. */
  credentialResolver?: CredentialResolver;
}

export interface PlaywrightRunResult {
  success: boolean;
  output?: Record<string, unknown>;
  errorMessage?: string;
  screenshotPath?: string;
  durationMs: number;
}

export interface IPlaywrightRunnerPort {
  run(options: PlaywrightRunOptions): Promise<PlaywrightRunResult>;
}
