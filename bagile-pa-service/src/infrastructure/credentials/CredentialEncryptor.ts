import { createCipheriv, createDecipheriv, randomBytes } from 'crypto';

const ALGORITHM = 'aes-256-gcm';
const IV_LEN = 12;

/**
 * AES-256-GCM encrypt/decrypt for credential values.
 * Requires a 64-hex-character key (32 bytes) via PA_ENCRYPTION_KEY env var.
 * Stored format: "<iv_hex>:<ciphertext_hex>:<auth_tag_hex>"
 */
export class CredentialEncryptor {
  private readonly key: Buffer;

  constructor(hexKey: string) {
    if (hexKey.length !== 64) {
      throw new Error('PA_ENCRYPTION_KEY must be 64 hex characters (32 bytes)');
    }
    this.key = Buffer.from(hexKey, 'hex');
  }

  static fromEnv(): CredentialEncryptor {
    const key = process.env['PA_ENCRYPTION_KEY'];
    if (!key) throw new Error('PA_ENCRYPTION_KEY is required for credential storage');
    return new CredentialEncryptor(key);
  }

  encrypt(plaintext: string): string {
    const iv = randomBytes(IV_LEN);
    const cipher = createCipheriv(ALGORITHM, this.key, iv);
    const ciphertext = Buffer.concat([cipher.update(plaintext, 'utf8'), cipher.final()]);
    const tag = cipher.getAuthTag();
    return `${iv.toString('hex')}:${ciphertext.toString('hex')}:${tag.toString('hex')}`;
  }

  decrypt(encoded: string): string {
    const parts = encoded.split(':');
    if (parts.length !== 3) throw new Error('Invalid encrypted credential format');
    const [ivHex, ciphertextHex, tagHex] = parts;
    const iv = Buffer.from(ivHex, 'hex');
    const ciphertext = Buffer.from(ciphertextHex, 'hex');
    const tag = Buffer.from(tagHex, 'hex');
    const decipher = createDecipheriv(ALGORITHM, this.key, iv);
    decipher.setAuthTag(tag);
    return decipher.update(ciphertext).toString('utf8') + decipher.final('utf8');
  }
}
