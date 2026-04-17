/**
 * Per-user credential store. Values are encrypted at rest.
 * Keys are plain identifiers (e.g. "scrumorg_username", "trello_api_key").
 */
export interface ICredentialStore {
  /** Returns the decrypted value, or undefined if not set. */
  get(userId: string, tenantId: string, key: string): Promise<string | undefined>;

  /** Encrypts and upserts the value. */
  set(userId: string, tenantId: string, key: string, value: string): Promise<void>;

  /** Returns only the key names (not values) for listing in UIs. */
  listKeys(userId: string, tenantId: string): Promise<string[]>;

  /** Removes the credential. Returns true if it existed. */
  delete(userId: string, tenantId: string, key: string): Promise<boolean>;
}

/**
 * Convenience type for a resolver function bound to a specific user.
 * Returns the credential value or undefined if not set.
 */
export type CredentialResolver = (key: string) => Promise<string | undefined>;
