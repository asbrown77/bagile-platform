export interface FormatInput {
  ok: boolean;
  data?: unknown;
  error?: string;
}

export function formatResult(result: FormatInput): string {
  if (!result.ok) return `Error: ${result.error}`;
  return JSON.stringify(result.data, null, 2);
}
