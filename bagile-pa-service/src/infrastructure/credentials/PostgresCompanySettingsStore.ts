import { Pool } from 'pg';
import type { ICompanySettingsStore } from '../../domain/ports/ICompanySettingsStore.js';
import { CredentialEncryptor } from './CredentialEncryptor.js';

export class PostgresCompanySettingsStore implements ICompanySettingsStore {
  private readonly encryptor: CredentialEncryptor;

  constructor(
    private readonly pool: Pool,
    encryptor?: CredentialEncryptor
  ) {
    this.encryptor = encryptor ?? CredentialEncryptor.fromEnv();
  }

  async get(tenantId: string, key: string): Promise<string | undefined> {
    const { rows } = await this.pool.query<{ value_enc: string }>(
      `SELECT value_enc FROM bagile.company_settings WHERE tenant_id = $1 AND key = $2`,
      [tenantId, key]
    );
    if (rows.length === 0) return undefined;
    return this.encryptor.decrypt(rows[0].value_enc);
  }

  async set(tenantId: string, key: string, value: string): Promise<void> {
    const encrypted = this.encryptor.encrypt(value);
    await this.pool.query(
      `INSERT INTO bagile.company_settings (tenant_id, key, value_enc)
       VALUES ($1, $2, $3)
       ON CONFLICT (tenant_id, key)
       DO UPDATE SET value_enc = EXCLUDED.value_enc, updated_at = now()`,
      [tenantId, key, encrypted]
    );
  }

  async listKeys(tenantId: string): Promise<string[]> {
    const { rows } = await this.pool.query<{ key: string }>(
      `SELECT key FROM bagile.company_settings WHERE tenant_id = $1 ORDER BY key`,
      [tenantId]
    );
    return rows.map((r) => r.key);
  }

  async delete(tenantId: string, key: string): Promise<boolean> {
    const { rowCount } = await this.pool.query(
      `DELETE FROM bagile.company_settings WHERE tenant_id = $1 AND key = $2`,
      [tenantId, key]
    );
    return (rowCount ?? 0) > 0;
  }
}
