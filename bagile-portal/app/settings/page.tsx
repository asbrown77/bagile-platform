"use client";

import { useCallback, useEffect, useState, useRef } from "react";
import { ApiKey, CreateKeyResponse, loginWithGoogle, listKeys, createKey, revokeKey } from "@/lib/api";

export default function Settings() {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<{ email: string; name: string } | null>(null);
  const [keys, setKeys] = useState<ApiKey[]>([]);
  const [newKey, setNewKey] = useState<CreateKeyResponse | null>(null);
  const [label, setLabel] = useState("");
  const [error, setError] = useState("");
  const btnRef = useRef<HTMLDivElement>(null);

  // Restore session
  useEffect(() => {
    const savedToken = localStorage.getItem("bagile_portal_token");
    const savedUser = localStorage.getItem("bagile_portal_user");
    if (savedToken && savedUser) {
      setToken(savedToken);
      setUser(JSON.parse(savedUser));
    }
  }, []);

  // Init Google sign-in if not logged in
  useEffect(() => {
    if (token) return;
    const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
    if (!clientId) return;

    (window as any).__settingsGoogleCallback = async (response: any) => {
      setError("");
      try {
        const data = await loginWithGoogle(response.credential);
        localStorage.setItem("bagile_portal_token", data.token);
        localStorage.setItem("bagile_portal_user", JSON.stringify({ email: data.email, name: data.name }));
        setToken(data.token);
        setUser({ email: data.email, name: data.name });
      } catch (err: any) {
        setError(err.message || "Login failed");
      }
    };

    let script = document.querySelector('script[src*="accounts.google.com/gsi/client"]') as HTMLScriptElement;
    if (!script) {
      script = document.createElement("script");
      script.src = "https://accounts.google.com/gsi/client";
      document.head.appendChild(script);
    }

    function init() {
      const g = (window as any).google;
      if (!g?.accounts?.id || !btnRef.current) return false;
      g.accounts.id.initialize({
        client_id: clientId,
        callback: (window as any).__settingsGoogleCallback,
      });
      btnRef.current.innerHTML = "";
      g.accounts.id.renderButton(btnRef.current, { theme: "outline", size: "large", text: "signin_with", width: 300 });
      return true;
    }

    if (!init()) {
      const interval = setInterval(() => { if (init()) clearInterval(interval); }, 300);
      setTimeout(() => clearInterval(interval), 10000);
    }
  }, [token]);

  const refreshKeys = useCallback(async () => {
    if (!token) return;
    try {
      setKeys(await listKeys(token));
    } catch { /* token expired */ }
  }, [token]);

  useEffect(() => { if (token) refreshKeys(); }, [token, refreshKeys]);

  async function handleCreateKey() {
    if (!token || !label.trim()) return;
    try {
      const result = await createKey(token, label.trim());
      setNewKey(result);
      setLabel("");
      await refreshKeys();
    } catch { setError("Failed to create key"); }
  }

  async function handleRevoke(id: string) {
    if (!token || !confirm("Revoke this key?")) return;
    try { await revokeKey(token, id); await refreshKeys(); }
    catch { setError("Failed to revoke key"); }
  }

  function handleSignOut() {
    localStorage.removeItem("bagile_portal_token");
    localStorage.removeItem("bagile_portal_user");
    setToken(null);
    setUser(null);
    setKeys([]);
  }

  // Not logged in
  if (!token) {
    return (
      <div className="max-w-3xl mx-auto py-10 px-4">
        <Nav />
        <div className="bg-white rounded-lg shadow-sm border p-8 text-center">
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Create MCP API Keys</h2>
          <p className="text-gray-600 text-sm mb-6">Sign in with Google to manage your API keys for the MCP server.</p>
          <div ref={btnRef} className="flex justify-center" />
          {error && <p className="mt-4 text-red-600 text-sm">{error}</p>}
        </div>
      </div>
    );
  }

  // Logged in
  return (
    <div className="max-w-3xl mx-auto py-10 px-4">
      <Nav user={user} onSignOut={handleSignOut} />

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>}

      {newKey && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
          <p className="font-semibold text-green-800 mb-1">Key created — copy it now!</p>
          <p className="text-sm text-green-700 mb-2">Paste into your MCP <code>.env</code> file. Never share in chat or email.</p>
          <div className="flex items-center gap-2">
            <code className="bg-white border px-3 py-2 rounded text-sm flex-1 font-mono break-all">{newKey.key}</code>
            <button onClick={() => navigator.clipboard.writeText(newKey.key)} className="bg-green-600 text-white px-3 py-2 rounded text-sm hover:bg-green-700">Copy</button>
          </div>
          <button onClick={() => setNewKey(null)} className="text-sm text-green-600 mt-2 hover:underline">Dismiss</button>
        </div>
      )}

      <div className="bg-white rounded-lg shadow-sm border p-4 mb-6">
        <h2 className="font-semibold text-gray-900 mb-3">Create API Key</h2>
        <div className="flex gap-2">
          <input type="text" placeholder="Label (e.g. MCP server)" value={label} onChange={(e) => setLabel(e.target.value)} onKeyDown={(e) => e.key === "Enter" && handleCreateKey()} className="border rounded px-3 py-2 flex-1 text-sm" />
          <button onClick={handleCreateKey} disabled={!label.trim()} className="bg-blue-600 text-white px-4 py-2 rounded text-sm hover:bg-blue-700 disabled:opacity-50">Create</button>
        </div>
      </div>

      <div className="bg-white rounded-lg shadow-sm border">
        <h2 className="font-semibold text-gray-900 p-4 border-b">Your API Keys</h2>
        {keys.length === 0 ? (
          <p className="p-4 text-gray-500 text-sm">No API keys yet.</p>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Label</th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Key</th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Created</th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">Last Used</th>
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
                  <td className="px-4 py-3 text-right">
                    {k.isactive ? <button onClick={() => handleRevoke(k.id)} className="text-red-500 hover:text-red-700 text-sm">Revoke</button> : <span className="text-gray-400 text-sm">Revoked</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

function Nav({ user, onSignOut }: { user?: { email: string; name: string } | null; onSignOut?: () => void } = {}) {
  return (
    <div className="flex items-center justify-between mb-8">
      <h1 className="text-2xl font-bold text-gray-900">BAgile</h1>
      <div className="flex gap-4 items-center text-sm">
        <a href="/dashboard" className="text-blue-600 hover:text-blue-800">Dashboard</a>
        <span className="font-medium border-b-2 border-blue-600 pb-0.5">Settings</span>
        {user && <span className="text-gray-400">{user.email}</span>}
        {onSignOut && <button onClick={onSignOut} className="text-gray-500 hover:text-gray-700">Sign out</button>}
      </div>
    </div>
  );
}
