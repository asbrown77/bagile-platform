"use client";

import { useCallback, useEffect, useState } from "react";
import {
  ApiKey,
  CreateKeyResponse,
  listKeys,
  createKey,
  revokeKey,
} from "@/lib/api";

export default function Settings() {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<{ email: string; name: string } | null>(null);
  const [keys, setKeys] = useState<ApiKey[]>([]);
  const [newKey, setNewKey] = useState<CreateKeyResponse | null>(null);
  const [label, setLabel] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    const savedToken = localStorage.getItem("bagile_portal_token");
    const savedUser = localStorage.getItem("bagile_portal_user");
    if (!savedToken) {
      window.location.href = "/login";
      return;
    }
    setToken(savedToken);
    if (savedUser) setUser(JSON.parse(savedUser));
  }, []);

  const refreshKeys = useCallback(async () => {
    if (!token) return;
    try {
      const data = await listKeys(token);
      setKeys(data);
    } catch {
      // Token might be expired
    }
  }, [token]);

  useEffect(() => {
    if (token) refreshKeys();
  }, [token, refreshKeys]);

  async function handleCreateKey() {
    if (!token || !label.trim()) return;
    setError("");
    try {
      const result = await createKey(token, label.trim());
      setNewKey(result);
      setLabel("");
      // Auto-save as the dashboard API key
      localStorage.setItem("bagile_api_key", result.key);
      await refreshKeys();
    } catch {
      setError("Failed to create key");
    }
  }

  async function handleRevoke(id: string) {
    if (!token || !confirm("Revoke this key? This cannot be undone.")) return;
    try {
      await revokeKey(token, id);
      await refreshKeys();
    } catch {
      setError("Failed to revoke key");
    }
  }

  function handleSignOut() {
    localStorage.removeItem("bagile_portal_token");
    localStorage.removeItem("bagile_portal_user");
    localStorage.removeItem("bagile_api_key");
    window.location.href = "/login";
  }

  if (!token) return null;

  return (
    <div className="max-w-3xl mx-auto py-10 px-4">
      <Nav user={user} onSignOut={handleSignOut} />

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>
      )}

      {/* New key banner */}
      {newKey && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
          <p className="font-semibold text-green-800 mb-1">Key created — save it now, you won't see it again!</p>
          <p className="text-sm text-green-700 mb-2">Paste it into your <code>.env</code> file. Never share in chat, email, or Slack.</p>
          <div className="flex items-center gap-2">
            <code className="bg-white border px-3 py-2 rounded text-sm flex-1 font-mono break-all">{newKey.key}</code>
            <button onClick={() => navigator.clipboard.writeText(newKey.key)} className="bg-green-600 text-white px-3 py-2 rounded text-sm hover:bg-green-700">Copy</button>
          </div>
          <p className="text-xs text-green-600 mt-2">This key has been saved for the dashboard automatically. <a href="/dashboard" className="underline">Go to Dashboard</a></p>
          <button onClick={() => setNewKey(null)} className="text-sm text-green-600 mt-1 hover:underline">Dismiss</button>
        </div>
      )}

      {/* Create key */}
      <div className="bg-white rounded-lg shadow-sm border p-4 mb-6">
        <h2 className="font-semibold text-gray-900 mb-3">Create API Key</h2>
        <div className="flex gap-2">
          <input type="text" placeholder="Label (e.g. MCP server, testing)" value={label} onChange={(e) => setLabel(e.target.value)} onKeyDown={(e) => e.key === "Enter" && handleCreateKey()} className="border rounded px-3 py-2 flex-1 text-sm" />
          <button onClick={handleCreateKey} disabled={!label.trim()} className="bg-blue-600 text-white px-4 py-2 rounded text-sm hover:bg-blue-700 disabled:opacity-50">Create</button>
        </div>
      </div>

      {/* Keys table */}
      <div className="bg-white rounded-lg shadow-sm border">
        <h2 className="font-semibold text-gray-900 p-4 border-b">Your API Keys</h2>
        {keys.length === 0 ? (
          <p className="p-4 text-gray-500 text-sm">No API keys yet. Create one above to access the dashboard.</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Label</th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Key</th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Created</th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Last Used</th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Status</th>
                <th className="px-4 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {keys.map((k) => (
                <tr key={k.id} className="border-t">
                  <td className="px-4 py-3">{k.label || "—"}</td>
                  <td className="px-4 py-3 font-mono text-gray-500">{k.keyprefix}...</td>
                  <td className="px-4 py-3 text-gray-500">{new Date(k.createdat).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-500">{k.lastusedat ? new Date(k.lastusedat).toLocaleDateString() : "Never"}</td>
                  <td className="px-4 py-3">
                    {k.isactive ? <span className="text-green-600 font-medium">Active</span> : <span className="text-red-500">Revoked</span>}
                  </td>
                  <td className="px-4 py-3 text-right">
                    {k.isactive && <button onClick={() => handleRevoke(k.id)} className="text-red-500 hover:text-red-700 text-sm">Revoke</button>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      <p className="text-center text-xs text-gray-400 mt-8">BAgile Portal v1.5.0</p>
    </div>
  );
}

function Nav({ user, onSignOut }: { user: { email: string; name: string } | null; onSignOut: () => void }) {
  return (
    <div className="flex items-center justify-between mb-8">
      <h1 className="text-2xl font-bold text-gray-900">BAgile</h1>
      <div className="flex gap-4 items-center text-sm">
        <a href="/dashboard" className="text-blue-600 hover:text-blue-800">Dashboard</a>
        <span className="font-medium border-b-2 border-blue-600 pb-0.5">Settings</span>
        <span className="text-gray-400">{user?.email}</span>
        <button onClick={onSignOut} className="text-gray-500 hover:text-gray-700">Sign out</button>
      </div>
    </div>
  );
}
