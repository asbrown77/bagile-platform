import { Pool } from 'pg';
import type { ICredentialStore } from '../../domain/ports/ICredentialStore.js';
import { CredentialEncryptor } from './CredentialEncryptor.js';

interface DbRow {
  id: string;
  user_id: string;
  tenant_id: string;
  key: string;
  value_enc: string;
  created_at: Date;
  updated_at: Date;
}

export class PostgresCredentialStore implements ICredentialStore {
  private readonly encryptor: CredentialEncryptor;

  constructor(
    private readonly pool: Pool,
    encryptor?: CredentialEncryptor
  ) {
    this.encryptor = encryptor ?? CredentialEncryptor.fromEnv();
  }

  async get(userId: string, tenantId: string, key: string): Promise<string | undefined> {
    const { rows } = await this.pool.query<DbRow>(
      `SELECT value_enc FROM bagile.pa_user_credentials
       WHERE user_id = $1 AND tenant_id = $2 AND key = $3`,
      [userId, tenantId, key]
    );
    if (rows.length === 0) return undefined;
    return this.encryptor.decrypt(rows[0].value_enc);
  }

  async set(userId: string, tenantId: string, key: string, value: string): Promise<void> {
    const encrypted = this.encryptor.encrypt(value);
    await this.pool.query(
      `INSERT INTO bagile.pa_user_credentials (user_id, tenant_id, key, value_enc)
       VALUES ($1, $2, $3, $4)
       ON CONFLICT (user_id, tenant_id, key)
       DO UPDATE SET value_enc = EXCLUDED.value_enc, updated_at = now()`,
      [userId, tenantId, key, encrypted]
    );
  }

  async listKeys(userId: string, tenantId: string): Promise<string[]> {
    const { rows } = await this.pool.query<{ key: string }>(
      `SELECT key FROM bagile.pa_user_credentials
       WHERE user_id = $1 AND tenant_id = $2
       ORDER BY key`,
      [userId, tenantId]
    );
    return rows.map((r) => r.key);
  }

  async delete(userId: string, tenantId: string, key: string): Promise<boolean> {
    const { rowCount } = await this.pool.query(
      `DELETE FROM bagile.pa_user_credentials
       WHERE user_id = $1 AND tenant_id = $2 AND key = $3`,
      [userId, tenantId, key]
    );
    return (rowCount ?? 0) > 0;
  }
}
