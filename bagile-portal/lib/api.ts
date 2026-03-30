const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk";

export interface ApiKey {
  id: string;
  keyprefix: string;
  label: string;
  owneremail: string;
  ownername: string;
  isactive: boolean;
  createdat: string;
  lastusedat: string | null;
  revokedat: string | null;
}

export interface CreateKeyResponse {
  id: string;
  key: string;
  prefix: string;
  label: string;
  message: string;
}

export async function loginWithGoogle(
  idToken: string
): Promise<{ token: string; email: string; name: string }> {
  const res = await fetch(`${API_URL}/portal/auth/google`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ idToken }),
  });

  if (!res.ok) {
    const err = await res.json().catch(() => ({}));
    throw new Error(err.error || `Login failed (${res.status})`);
  }

  return res.json();
}

export async function listKeys(token: string): Promise<ApiKey[]> {
  const res = await fetch(`${API_URL}/portal/keys`, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to list keys");
  return res.json();
}

export async function createKey(
  token: string,
  label: string
): Promise<CreateKeyResponse> {
  const res = await fetch(`${API_URL}/portal/keys`, {
    method: "POST",
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ label }),
  });
  if (!res.ok) throw new Error("Failed to create key");
  return res.json();
}

export async function revokeKey(
  token: string,
  id: string
): Promise<void> {
  const res = await fetch(`${API_URL}/portal/keys/${id}`, {
    method: "DELETE",
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to revoke key");
}
