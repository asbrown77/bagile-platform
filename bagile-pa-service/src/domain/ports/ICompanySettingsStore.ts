/**
 * Per-tenant company settings store. Values are encrypted at rest.
 * Keys are plain identifiers (e.g. "bagile_api_key", "xero_client_id").
 */
export interface ICompanySettingsStore {
  get(tenantId: string, key: string): Promise<string | undefined>;
  set(tenantId: string, key: string, value: string): Promise<void>;
  listKeys(tenantId: string): Promise<string[]>;
  delete(tenantId: string, key: string): Promise<boolean>;
}

export type CompanySettingsResolver = (key: string) => Promise<string | undefined>;
