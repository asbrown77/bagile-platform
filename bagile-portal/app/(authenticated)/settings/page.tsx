"use client";

import { useCallback, useEffect, useState, useRef } from "react";
import { ApiKey, CreateKeyResponse, loginWithGoogle, listKeys, createKey, revokeKey } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { Key, Plus } from "lucide-react";

export default function Settings() {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<{ email: string; name: string } | null>(null);
  const [keys, setKeys] = useState<ApiKey[]>([]);
  const [newKey, setNewKey] = useState<CreateKeyResponse | null>(null);
  const [label, setLabel] = useState("");
  const [error, setError] = useState("");
  const btnRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const savedToken = localStorage.getItem("bagile_portal_token");
    const savedUser = localStorage.getItem("bagile_portal_user");
    if (savedToken && savedUser) {
      setToken(savedToken);
      setUser(JSON.parse(savedUser));
    }
  }, []);

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
      g.accounts.id.initialize({ client_id: clientId, callback: (window as any).__settingsGoogleCallback });
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
    try { setKeys(await listKeys(token)); } catch {}
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
    try { await revokeKey(token, id); await refreshKeys(); } catch { setError("Failed to revoke key"); }
  }

  if (!token) {
    return (
      <>
        <PageHeader title="API Keys" subtitle="Sign in to manage your API keys" />
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 text-center max-w-md mx-auto">
          <Key className="w-10 h-10 text-gray-300 mx-auto mb-4" />
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Create MCP API Keys</h2>
          <p className="text-gray-500 text-sm mb-6">Sign in with Google to manage your API keys.</p>
          <div ref={btnRef} className="flex justify-center" />
          {error && <p className="mt-4 text-red-600 text-sm">{error}</p>}
        </div>
      </>
    );
  }

  return (
    <>
      <PageHeader title="API Keys" subtitle={user?.email || ""} />

      {error && <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>}

      {newKey && (
        <div className="mb-6">
          <AlertBanner variant="success">
            <p className="font-semibold mb-1">Key created — copy it now!</p>
            <div className="flex items-center gap-2 mt-2">
              <code className="bg-white border px-3 py-2 rounded text-sm flex-1 font-mono break-all">{newKey.key}</code>
              <Button size="sm" onClick={() => navigator.clipboard.writeText(newKey.key)}>Copy</Button>
            </div>
            <button onClick={() => setNewKey(null)} className="text-sm mt-2 underline">Dismiss</button>
          </AlertBanner>
        </div>
      )}

      {/* Create key */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 mb-6">
        <h2 className="text-sm font-semibold text-gray-900 mb-3">Create API Key</h2>
        <div className="flex gap-2">
          <input
            type="text"
            placeholder="Label (e.g. MCP server)"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleCreateKey()}
            className="border border-gray-300 rounded-lg px-3 py-2 flex-1 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
          />
          <Button onClick={handleCreateKey} disabled={!label.trim()} size="md">
            <Plus className="w-4 h-4" /> Create
          </Button>
        </div>
      </div>

      {/* Keys list */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-5 py-3 border-b border-gray-200">
          <h2 className="text-sm font-semibold text-gray-900">Your API Keys</h2>
        </div>
        {keys.length === 0 ? (
          <EmptyState icon={<Key className="w-10 h-10" />} title="No API keys" description="Create your first key above" />
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Label</th>
                <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Key</th>
                <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Created</th>
                <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Last Used</th>
                <th className="px-4 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {keys.map((k) => (
                <tr key={k.id} className="border-t border-gray-100">
                  <td className="px-4 py-3">{k.label || "—"}</td>
                  <td className="px-4 py-3 font-mono text-gray-500">{k.keyprefix}...</td>
                  <td className="px-4 py-3 text-gray-500 hidden md:table-cell">{new Date(k.createdat).toLocaleDateString()}</td>
                  <td className="px-4 py-3 text-gray-500 hidden md:table-cell">{k.lastusedat ? new Date(k.lastusedat).toLocaleDateString() : "Never"}</td>
                  <td className="px-4 py-3 text-right">
                    {k.isactive ? (
                      <button onClick={() => handleRevoke(k.id)} className="text-red-500 hover:text-red-700 text-xs font-medium">Revoke</button>
                    ) : (
                      <span className="text-gray-400 text-xs">Revoked</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </>
  );
}
