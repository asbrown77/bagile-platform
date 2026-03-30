"use client";

import { useCallback, useEffect, useState } from "react";
import {
  ApiKey,
  CreateKeyResponse,
  loginWithGoogle,
  listKeys,
  createKey,
  revokeKey,
} from "@/lib/api";

export default function Home() {
  const [token, setToken] = useState<string | null>(null);
  const [user, setUser] = useState<{ email: string; name: string } | null>(
    null
  );
  const [keys, setKeys] = useState<ApiKey[]>([]);
  const [newKey, setNewKey] = useState<CreateKeyResponse | null>(null);
  const [label, setLabel] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);

  const refreshKeys = useCallback(async () => {
    if (!token) return;
    try {
      const data = await listKeys(token);
      setKeys(data);
    } catch {
      setError("Failed to load keys");
    }
  }, [token]);

  useEffect(() => {
    if (token) refreshKeys();
  }, [token, refreshKeys]);

  // Initialize Google Sign-In
  useEffect(() => {
    const clientId = process.env.NEXT_PUBLIC_GOOGLE_CLIENT_ID;
    if (!clientId) return;

    const initGoogle = () => {
      if (!(window as any).google?.accounts) {
        setTimeout(initGoogle, 100);
        return;
      }
      (window as any).google.accounts.id.initialize({
        client_id: clientId,
        callback: handleGoogleResponse,
      });
      (window as any).google.accounts.id.renderButton(
        document.getElementById("google-btn"),
        { theme: "outline", size: "large", text: "signin_with", width: 300 }
      );
    };
    initGoogle();
  }, []);

  async function handleGoogleResponse(response: { credential: string }) {
    setError("");
    setLoading(true);
    try {
      const data = await loginWithGoogle(response.credential);
      setToken(data.token);
      setUser({ email: data.email, name: data.name });
    } catch (err: any) {
      setError(err.message || "Login failed");
    } finally {
      setLoading(false);
    }
  }

  // Make handleGoogleResponse available globally for Google callback
  useEffect(() => {
    (window as any).handleGoogleResponse = handleGoogleResponse;
  }, []);

  async function handleCreateKey() {
    if (!token || !label.trim()) return;
    setError("");
    try {
      const result = await createKey(token, label.trim());
      setNewKey(result);
      setLabel("");
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

  function copyToClipboard(text: string) {
    navigator.clipboard.writeText(text);
  }

  // Not logged in
  if (!token) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="bg-white rounded-lg shadow-md p-8 max-w-md w-full text-center">
          <h1 className="text-2xl font-bold text-gray-900 mb-2">
            BAgile Portal
          </h1>
          <p className="text-gray-600 mb-6">
            Sign in to manage your API keys
          </p>
          <div id="google-btn" className="flex justify-center" />
          {loading && (
            <p className="mt-4 text-gray-500">Signing in...</p>
          )}
          {error && (
            <p className="mt-4 text-red-600 text-sm">{error}</p>
          )}
        </div>
      </div>
    );
  }

  // Logged in
  return (
    <div className="max-w-3xl mx-auto py-10 px-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">BAgile Portal</h1>
          <p className="text-gray-600 text-sm">{user?.email}</p>
        </div>
        <button
          onClick={() => {
            setToken(null);
            setUser(null);
            setKeys([]);
          }}
          className="text-sm text-gray-500 hover:text-gray-700"
        >
          Sign out
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
          {error}
        </div>
      )}

      {/* New key created banner */}
      {newKey && (
        <div className="bg-green-50 border border-green-200 rounded-lg p-4 mb-6">
          <p className="font-semibold text-green-800 mb-2">
            Key created — save it now, you won't see it again!
          </p>
          <div className="flex items-center gap-2">
            <code className="bg-white border px-3 py-2 rounded text-sm flex-1 font-mono break-all">
              {newKey.key}
            </code>
            <button
              onClick={() => copyToClipboard(newKey.key)}
              className="bg-green-600 text-white px-3 py-2 rounded text-sm hover:bg-green-700"
            >
              Copy
            </button>
          </div>
          <button
            onClick={() => setNewKey(null)}
            className="text-sm text-green-600 mt-2 hover:underline"
          >
            Dismiss
          </button>
        </div>
      )}

      {/* Create key form */}
      <div className="bg-white rounded-lg shadow-sm border p-4 mb-6">
        <h2 className="font-semibold text-gray-900 mb-3">Create API Key</h2>
        <div className="flex gap-2">
          <input
            type="text"
            placeholder="Label (e.g. MCP server, testing)"
            value={label}
            onChange={(e) => setLabel(e.target.value)}
            onKeyDown={(e) => e.key === "Enter" && handleCreateKey()}
            className="border rounded px-3 py-2 flex-1 text-sm"
          />
          <button
            onClick={handleCreateKey}
            disabled={!label.trim()}
            className="bg-blue-600 text-white px-4 py-2 rounded text-sm hover:bg-blue-700 disabled:opacity-50"
          >
            Create
          </button>
        </div>
      </div>

      {/* Keys table */}
      <div className="bg-white rounded-lg shadow-sm border">
        <h2 className="font-semibold text-gray-900 p-4 border-b">
          Your API Keys
        </h2>
        {keys.length === 0 ? (
          <p className="p-4 text-gray-500 text-sm">
            No API keys yet. Create one above.
          </p>
        ) : (
          <table className="w-full text-sm">
            <thead className="bg-gray-50">
              <tr>
                <th className="text-left px-4 py-2 font-medium text-gray-600">
                  Label
                </th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">
                  Key
                </th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">
                  Created
                </th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">
                  Last Used
                </th>
                <th className="text-left px-4 py-2 font-medium text-gray-600">
                  Status
                </th>
                <th className="px-4 py-2"></th>
              </tr>
            </thead>
            <tbody>
              {keys.map((k) => (
                <tr key={k.id} className="border-t">
                  <td className="px-4 py-3">{k.label || "—"}</td>
                  <td className="px-4 py-3 font-mono text-gray-500">
                    {k.keyprefix}...
                  </td>
                  <td className="px-4 py-3 text-gray-500">
                    {new Date(k.createdat).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-gray-500">
                    {k.lastusedat
                      ? new Date(k.lastusedat).toLocaleDateString()
                      : "Never"}
                  </td>
                  <td className="px-4 py-3">
                    {k.isactive ? (
                      <span className="text-green-600 font-medium">
                        Active
                      </span>
                    ) : (
                      <span className="text-red-500">Revoked</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-right">
                    {k.isactive && (
                      <button
                        onClick={() => handleRevoke(k.id)}
                        className="text-red-500 hover:text-red-700 text-sm"
                      >
                        Revoke
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* MCP setup instructions */}
      <div className="bg-white rounded-lg shadow-sm border p-4 mt-6">
        <h2 className="font-semibold text-gray-900 mb-2">
          MCP Server Setup
        </h2>
        <p className="text-sm text-gray-600 mb-3">
          After creating a key, configure your Claude Desktop or Claude Code
          to use the BAgile MCP server:
        </p>
        <pre className="bg-gray-900 text-gray-100 rounded p-3 text-xs overflow-x-auto">
{`// Claude Desktop: ~/.claude/claude_desktop_config.json
{
  "mcpServers": {
    "bagile": {
      "command": "node",
      "args": ["/path/to/bagile-mcp-server/dist/index.js"],
      "env": {
        "BAGILE_API_URL": "https://api.bagile.co.uk",
        "BAGILE_API_KEY": "your-key-here"
      }
    }
  }
}`}
        </pre>
      </div>
    </div>
  );
}
