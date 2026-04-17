"use client";

import { Suspense, useCallback, useEffect, useState, useRef } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { ApiKey, CreateKeyResponse, PortalAuthError, loginWithGoogle, listKeys, createKey, revokeKey, PostCourseTemplate, listPostCourseTemplates, upsertPostCourseTemplate, PreCourseTemplate, getPreCourseTemplates, updatePreCourseTemplate, Trainer, getTrainers, createTrainer, updateTrainer, deleteTrainer, getServiceConfig, setServiceConfig } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { Key, Plus, Settings2, FileText, ChevronLeft, Save, Eye, Code, Users, Pencil, Trash2, X, Check, Bot, Shield, Zap, Globe } from "lucide-react";
import { loadConfig, saveConfig, type PortalConfig } from "@/lib/config";
import { CourseDefsEditor } from "@/components/courses/CourseDefsEditor";

// ── Tab definitions ───────────────────────────────────────

type Tab = "general" | "post-course" | "pre-course" | "trainers" | "integrations" | "courses" | "claude-pa";

const TABS: { id: Tab; label: string }[] = [
  { id: "general",      label: "General" },
  { id: "post-course",  label: "Post-Course" },
  { id: "pre-course",   label: "Pre-Course" },
  { id: "trainers",     label: "Trainers" },
  { id: "integrations", label: "Integrations" },
  { id: "courses",      label: "Courses" },
  { id: "claude-pa",    label: "Claude PA" },
];

// ── Root export (wraps in Suspense for useSearchParams) ───

export default function Settings() {
  return (
    <Suspense>
      <SettingsContent />
    </Suspense>
  );
}

// ── Main settings component ───────────────────────────────

function SettingsContent() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const rawTab = searchParams.get("tab") as Tab | null;
  const activeTab: Tab = TABS.some((t) => t.id === rawTab) ? rawTab! : "general";

  function setTab(tab: Tab) {
    const params = new URLSearchParams(searchParams.toString());
    params.set("tab", tab);
    router.replace(`?${params.toString()}`);
  }

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
        if (data.apiKey) localStorage.setItem("bagile_api_key", data.apiKey);
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

  function clearSession() {
    localStorage.removeItem("bagile_portal_token");
    setToken(null);
    setUser(null);
    setKeys([]);
    setError("Session expired — please sign in again.");
  }

  const refreshKeys = useCallback(async () => {
    if (!token) return;
    try { setKeys(await listKeys(token)); }
    catch (err) { if (err instanceof PortalAuthError) clearSession(); }
  }, [token]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => { if (token) refreshKeys(); }, [token, refreshKeys]);

  async function handleCreateKey() {
    if (!token || !label.trim()) return;
    try {
      const result = await createKey(token, label.trim());
      setNewKey(result);
      setLabel("");
      // Auto-save as the dashboard API key
      localStorage.setItem("bagile_api_key", result.key);
      await refreshKeys();
    } catch (err) {
      if (err instanceof PortalAuthError) clearSession();
      else setError("Failed to create key");
    }
  }

  async function handleRevoke(id: string) {
    if (!token || !confirm("Revoke this key?")) return;
    try { await revokeKey(token, id); await refreshKeys(); }
    catch (err) {
      if (err instanceof PortalAuthError) clearSession();
      else setError("Failed to revoke key");
    }
  }

  return (
    <>
      <PageHeader
        title="Settings"
        subtitle={token ? (user?.email || "") : "Sign in to manage your API keys"}
      />

      {/* Tab bar */}
      <div className="flex rounded-lg border border-gray-300 overflow-hidden mb-6 w-fit">
        {TABS.map((tab, i) => (
          <button
            key={tab.id}
            onClick={() => setTab(tab.id)}
            className={`px-4 py-2 text-sm font-medium transition-colors
              ${i > 0 ? "border-l border-gray-300" : ""}
              ${activeTab === tab.id
                ? "bg-brand-600 text-white"
                : "bg-white text-gray-600 hover:bg-gray-50"}`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* General tab */}
      {activeTab === "general" && (
        <>
          {error && <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>}

          {/* API Keys section */}
          {!token ? (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 text-center max-w-md mx-auto mb-6">
              <Key className="w-10 h-10 text-gray-300 mx-auto mb-4" />
              <h2 className="text-lg font-semibold text-gray-900 mb-2">Create MCP API Keys</h2>
              <p className="text-gray-500 text-sm mb-6">Sign in with Google to manage your API keys.</p>
              <div ref={btnRef} className="flex justify-center" />
              {error && <p className="mt-4 text-red-600 text-sm">{error}</p>}
            </div>
          ) : (
            <>
              {newKey && (
                <div className="mb-6 space-y-4">
                  <AlertBanner variant="success">
                    <p className="font-semibold mb-1">Key created — copy it now! This is the only time you'll see the full key.</p>
                    <div className="flex items-center gap-2 mt-2">
                      <code className="bg-white border px-3 py-2 rounded text-sm flex-1 font-mono break-all">{newKey.key}</code>
                      <Button size="sm" onClick={() => navigator.clipboard.writeText(newKey.key)}>Copy</Button>
                    </div>
                    <button onClick={() => setNewKey(null)} className="text-sm mt-2 underline">Dismiss</button>
                  </AlertBanner>

                  {/* MCP Setup Instructions */}
                  <div className="bg-gray-900 rounded-xl p-5 text-sm">
                    <p className="text-gray-300 font-medium mb-3">To use with Claude Code (MCP), add this to your project's <code className="text-amber-400">.mcp.json</code>:</p>
                    <pre className="bg-gray-950 rounded-lg p-4 text-gray-300 overflow-x-auto text-xs leading-relaxed">
{`{
  "mcpServers": {
    "bagile-api": {
      "command": "node",
      "args": ["path/to/bagile-mcp-server/dist/index.js"],
      "env": {
        "BAGILE_API_URL": "https://api.bagile.co.uk",
        "BAGILE_API_KEY": "${newKey.key}"
      }
    }
  }
}`}
                    </pre>
                    <p className="text-gray-500 text-xs mt-3">This gives Claude Code access to courses, orders, students, revenue, and analytics via 20+ MCP tools.</p>
                  </div>
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

              {/* MCP Setup Guide */}
              <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden mt-6">
                <div className="px-5 py-3 border-b border-gray-200">
                  <h2 className="text-sm font-semibold text-gray-900">Using with AI Assistants (MCP)</h2>
                </div>
                <div className="p-5 text-sm text-gray-600 space-y-4">
                  <p>
                    The BAgile API can be used as an MCP (Model Context Protocol) server with Claude Code,
                    Cursor, Windsurf, or any MCP-compatible AI assistant. This gives your AI access to query
                    courses, orders, students, revenue analytics, and more.
                  </p>

                  <div>
                    <p className="font-medium text-gray-900 mb-2">Setup steps:</p>
                    <ol className="list-decimal list-inside space-y-1.5 text-gray-600">
                      <li>Create an API key above (label it "MCP" or "Claude Code")</li>
                      <li>Clone the MCP server: <code className="bg-gray-100 px-1.5 py-0.5 rounded text-xs">bagile-mcp-server/</code> from the repo</li>
                      <li>Run <code className="bg-gray-100 px-1.5 py-0.5 rounded text-xs">npm install && npm run build</code></li>
                      <li>Add the config below to your project's <code className="bg-gray-100 px-1.5 py-0.5 rounded text-xs">.mcp.json</code></li>
                    </ol>
                  </div>

                  <pre className="bg-gray-900 rounded-lg p-4 text-gray-300 overflow-x-auto text-xs leading-relaxed">
{`// .mcp.json (in your project root)
{
  "mcpServers": {
    "bagile-api": {
      "command": "node",
      "args": ["path/to/bagile-mcp-server/dist/index.js"],
      "env": {
        "BAGILE_API_URL": "https://api.bagile.co.uk",
        "BAGILE_API_KEY": "your-api-key-here"
      }
    }
  }
}`}
                  </pre>

                  <div>
                    <p className="font-medium text-gray-900 mb-2">Available MCP tools ({20}):</p>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-x-6 gap-y-1 text-xs text-gray-500">
                      <span>list_course_schedules — Filter courses by date, type, trainer</span>
                      <span>get_course_attendees — Attendees with billing details</span>
                      <span>get_course_monitoring — At-risk courses, decision deadlines</span>
                      <span>list_orders — Orders with status, date, email filters</span>
                      <span>get_order — Full order detail with line items</span>
                      <span>list_students — Search by name, email, organisation</span>
                      <span>get_student_enrolments — Student's course history</span>
                      <span>list_organisations — Company lookup</span>
                      <span>get_organisation_course_history — What they've booked</span>
                      <span>list_transfers / get_pending_transfers — Transfer management</span>
                      <span>get_revenue_summary — Monthly, YoY, by type, by country</span>
                      <span>get_partner_analytics — PTN tier tracking</span>
                      <span>cancel_course — Cancel with reason</span>
                      <span>health_check — API status</span>
                    </div>
                  </div>

                  <p className="text-xs text-gray-400">
                    Once configured, ask Claude: "Show me at-risk courses" or "What's our revenue this month?" —
                    it will use the MCP tools to query your live BAgile data.
                  </p>
                </div>
              </div>
            </>
          )}

          {/* Course Risk Thresholds — always visible on General tab */}
          <CourseThresholds />
        </>
      )}

      {/* Post-Course tab */}
      {activeTab === "post-course" && <PostCourseTemplatesEditor />}

      {/* Pre-Course tab */}
      {activeTab === "pre-course" && <PreCourseTemplatesEditor />}

      {/* Trainers tab */}
      {activeTab === "trainers" && <TrainersEditor />}

      {/* Integrations tab */}
      {activeTab === "integrations" && <IntegrationsEditor />}

      {/* Courses tab */}
      {activeTab === "courses" && <CourseDefsEditor />}

      {/* Claude PA tab */}
      {activeTab === "claude-pa" && <ClaudePaDocs />}
    </>
  );
}

function CourseThresholds() {
  const [config, setConfig] = useState<PortalConfig | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => { setConfig(loadConfig()); }, []);

  function handleSave() {
    if (!config) return;
    saveConfig(config);
    setSaved(true);
    setTimeout(() => setSaved(false), 2000);
  }

  if (!config) return null;

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6 mt-6">
      <div className="flex items-center gap-3 mb-4">
        <div className="p-2 bg-amber-50 rounded-lg">
          <Settings2 className="w-5 h-5 text-amber-600" />
        </div>
        <div>
          <h2 className="text-lg font-semibold text-gray-900">Course Risk Thresholds</h2>
          <p className="text-sm text-gray-500">Configure when courses are flagged as "at risk" or "cancel"</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            At-risk threshold (days before start)
          </label>
          <input
            type="number"
            min={0}
            max={30}
            value={config.atRiskDays}
            onChange={(e) => setConfig({ ...config, atRiskDays: parseInt(e.target.value) || 0 })}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
          />
          <p className="text-xs text-gray-400 mt-1">
            Courses with low enrolment within this many days of start date are flagged "at risk". Set to 0 to disable.
          </p>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1">
            Minimum enrolments (for "guaranteed")
          </label>
          <input
            type="number"
            min={1}
            max={50}
            value={config.minEnrolments}
            onChange={(e) => setConfig({ ...config, minEnrolments: parseInt(e.target.value) || 1 })}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
          />
          <p className="text-xs text-gray-400 mt-1">
            Courses below this enrolment count are considered "low enrolment" for risk assessment.
          </p>
        </div>
      </div>

      <div className="flex items-center gap-3 mt-4">
        <Button onClick={handleSave}>Save thresholds</Button>
        {saved && <span className="text-sm text-green-600 font-medium">Saved!</span>}
      </div>

      <div className="mt-4 p-3 bg-gray-50 rounded-lg text-xs text-gray-500">
        <strong>How it works:</strong> Courses at <strong>0 days</strong> with low enrolment → "Cancel".
        Courses within <strong>{config.atRiskDays} day{config.atRiskDays !== 1 ? "s" : ""}</strong> with
        fewer than <strong>{config.minEnrolments}</strong> enrolments → "At Risk".
      </div>
    </div>
  );
}

// ── Integrations Editor ───────────────────────────────────────

const MASK = "********";

interface IntegrationField {
  key: string;
  label: string;
  type: "text" | "password";
}

interface IntegrationSection {
  title: string;
  description: string;
  fields: IntegrationField[];
}

const INTEGRATION_SECTIONS: IntegrationSection[] = [
  {
    title: "WooCommerce REST API",
    description: "WordPress Application Password for the REST API — used to create and update course products programmatically.",
    fields: [
      { key: "woocommerce.consumer_key",    label: "WP Username",           type: "text" },
      { key: "woocommerce.consumer_secret", label: "WP Application Password", type: "password" },
    ],
  },
  {
    title: "WordPress Admin (Playwright)",
    description: "Admin login for browser automation — used for FooEvents ticket management and wp-admin operations.",
    fields: [
      { key: "woocommerce.admin_username", label: "Admin Username", type: "text" },
      { key: "woocommerce.admin_password", label: "Admin Password", type: "password" },
    ],
  },
  {
    title: "Scrum.org",
    description: "Login credentials used by the automated course listing script.",
    fields: [
      { key: "scrumorg.username", label: "Username", type: "text" },
      { key: "scrumorg.password", label: "Password", type: "password" },
    ],
  },
];

function IntegrationsEditor() {
  const [apiKey, setApiKey] = useState<string>("");
  // fieldValues holds the current display value for each key
  const [fieldValues, setFieldValues] = useState<Record<string, string>>({});
  // dirtyKeys tracks which fields the user has actually typed into
  const [dirtyKeys, setDirtyKeys] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState<Record<string, boolean>>({});
  const [saved, setSaved] = useState<Record<string, boolean>>({});
  const [saveError, setSaveError] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const key = localStorage.getItem("bagile_api_key") ?? "";
    setApiKey(key);
  }, []);

  const loadConfig = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);
    try {
      const data = await getServiceConfig(apiKey);
      // For masked values, show placeholder; for others show actual value.
      const initial: Record<string, string> = {};
      for (const [k, v] of Object.entries(data)) {
        initial[k] = v; // may be "********" or ""
      }
      setFieldValues(initial);
      setDirtyKeys(new Set());
    } catch {
      // silently skip — api key may not be set yet
    } finally {
      setLoading(false);
    }
  }, [apiKey]);

  useEffect(() => { loadConfig(); }, [loadConfig]);

  function handleChange(key: string, value: string) {
    setFieldValues((prev) => ({ ...prev, [key]: value }));
    setDirtyKeys((prev) => new Set(prev).add(key));
  }

  async function handleSaveSection(section: IntegrationSection) {
    const sectionKey = section.title;
    setSaving((s) => ({ ...s, [sectionKey]: true }));
    setSaveError((e) => ({ ...e, [sectionKey]: "" }));
    try {
      for (const field of section.fields) {
        if (!dirtyKeys.has(field.key)) continue; // not changed — skip
        const value = fieldValues[field.key] ?? "";
        if (value === MASK) continue; // mask sentinel — skip
        await setServiceConfig(apiKey, field.key, value);
      }
      setSaved((s) => ({ ...s, [sectionKey]: true }));
      setDirtyKeys((prev) => {
        const next = new Set(prev);
        section.fields.forEach((f) => next.delete(f.key));
        return next;
      });
      setTimeout(() => setSaved((s) => ({ ...s, [sectionKey]: false })), 2500);
      // Reload to get fresh masks from API
      await loadConfig();
    } catch (err: any) {
      setSaveError((e) => ({ ...e, [sectionKey]: err?.message ?? "Failed to save" }));
    } finally {
      setSaving((s) => ({ ...s, [sectionKey]: false }));
    }
  }

  if (!apiKey) {
    return (
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 text-center max-w-md">
        <p className="text-sm text-gray-500">Set your API key on the General tab to manage integrations.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {INTEGRATION_SECTIONS.map((section) => {
        const sectionKey = section.title;
        const isSaving = saving[sectionKey] ?? false;
        const wasSaved = saved[sectionKey] ?? false;
        const error = saveError[sectionKey] ?? "";
        const hasDirtyField = section.fields.some((f) => dirtyKeys.has(f.key));

        return (
          <div key={sectionKey} className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 mb-1">{section.title}</h2>
            <p className="text-sm text-gray-500 mb-4">{section.description}</p>

            {loading ? (
              <p className="text-sm text-gray-400">Loading...</p>
            ) : (
              <div className="space-y-3">
                {section.fields.map((field) => {
                  const currentVal = fieldValues[field.key] ?? "";
                  const isMasked = currentVal === MASK && !dirtyKeys.has(field.key);
                  return (
                    <div key={field.key}>
                      <label className="block text-sm font-medium text-gray-700 mb-1">
                        {field.label}
                      </label>
                      <input
                        type={field.type}
                        className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500 font-mono"
                        value={isMasked ? "" : currentVal}
                        placeholder={isMasked ? MASK : ""}
                        onChange={(e) => handleChange(field.key, e.target.value)}
                        autoComplete="off"
                      />
                    </div>
                  );
                })}
              </div>
            )}

            {error && <p className="mt-2 text-sm text-red-600">{error}</p>}

            <div className="flex items-center gap-3 mt-4">
              <Button
                onClick={() => handleSaveSection(section)}
                disabled={isSaving || !hasDirtyField || loading}
                size="md"
              >
                <Save className="w-4 h-4" />
                {isSaving ? "Saving..." : "Save"}
              </Button>
              {wasSaved && <span className="text-sm text-green-600 font-medium">Saved!</span>}
            </div>
          </div>
        );
      })}
    </div>
  );
}

// ── Post-Course Templates Editor ──────────────────────────────

type EditorState =
  | { view: "list" }
  | { view: "edit"; template: PostCourseTemplate };

function PostCourseTemplatesEditor() {
  const [apiKey, setApiKey] = useState<string>("");
  const [templates, setTemplates] = useState<PostCourseTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  const [state, setState] = useState<EditorState>({ view: "list" });
  const [editSubject, setEditSubject] = useState("");
  const [editBody, setEditBody] = useState("");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [showPreview, setShowPreview] = useState(false);

  useEffect(() => {
    const key = localStorage.getItem("bagile_api_key") ?? "";
    setApiKey(key);
  }, []);

  /** Normalise course type keys so that legacy variants from different seed
   *  versions collapse to a single canonical key (e.g. "PALE" → "PAL-E").
   *  The most recently updated entry wins when two keys map to the same name. */
  function deduplicateTemplates(data: PostCourseTemplate[]): PostCourseTemplate[] {
    const canonical: Record<string, string> = {
      PALE: "PAL-E",
      PSMAI: "PSM-AI",
      PSPOAI: "PSPO-AI",
      APSSD: "APS-SD",
      PSMA: "PSM-A",
      PSPOA: "PSPO-A",
    };
    const seen = new Map<string, PostCourseTemplate>();
    // Sort newest-first so the most recently updated entry wins on collision
    const sorted = [...data].sort(
      (a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime()
    );
    for (const t of sorted) {
      const key = canonical[t.courseType] ?? t.courseType;
      if (!seen.has(key)) {
        // Normalise the courseType to the canonical form so the UI shows it correctly
        seen.set(key, { ...t, courseType: key });
      }
    }
    return Array.from(seen.values()).sort((a, b) => a.courseType.localeCompare(b.courseType));
  }

  const loadTemplates = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);
    try {
      const data = await listPostCourseTemplates(apiKey);
      setTemplates(deduplicateTemplates(data));
    } catch {
      // silently skip — api key may not be set yet
    } finally {
      setLoading(false);
    }
  }, [apiKey]);

  useEffect(() => { loadTemplates(); }, [loadTemplates]);

  function openEdit(template: PostCourseTemplate) {
    setEditSubject(template.subjectTemplate);
    setEditBody(template.htmlBody);
    setSaveError("");
    setSaveSuccess(false);
    setShowPreview(false);
    setState({ view: "edit", template });
  }

  async function handleSave() {
    if (state.view !== "edit") return;
    setSaving(true);
    setSaveError("");
    setSaveSuccess(false);
    try {
      await upsertPostCourseTemplate(apiKey, state.template.courseType, editSubject, editBody);
      setSaveSuccess(true);
      await loadTemplates();
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err: any) {
      setSaveError(err.message ?? "Failed to save template");
    } finally {
      setSaving(false);
    }
  }

  const isPlaceholder = (t: PostCourseTemplate) =>
    t.htmlBody.includes("needs customising");

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="px-5 py-3 border-b border-gray-200 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-blue-50 rounded-lg">
            <FileText className="w-5 h-5 text-blue-600" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Post-Course Email Templates</h2>
            <p className="text-sm text-gray-500">Edit the follow-up emails sent after each course type</p>
          </div>
        </div>
        {state.view === "edit" && (
          <button
            onClick={() => setState({ view: "list" })}
            className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-900"
          >
            <ChevronLeft className="w-4 h-4" /> Back to list
          </button>
        )}
      </div>

      {state.view === "list" && (
        <div>
          {loading && (
            <div className="p-6 text-center text-sm text-gray-400">Loading templates...</div>
          )}
          {!loading && !apiKey && (
            <div className="p-6 text-center text-sm text-gray-400">
              Set your API key on the General tab to manage templates.
            </div>
          )}
          {!loading && apiKey && templates.length === 0 && (
            <EmptyState
              icon={<FileText className="w-10 h-10" />}
              title="No templates found"
              description="Run the V39 migration to seed templates, then reload."
            />
          )}
          {!loading && templates.length > 0 && (
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Course Type</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Subject</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Last Updated</th>
                  <th className="px-4 py-2"></th>
                </tr>
              </thead>
              <tbody>
                {templates.map((t) => (
                  <tr key={t.courseType} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">
                      {t.courseType}
                      {isPlaceholder(t) && (
                        <span className="ml-2 text-xs bg-amber-100 text-amber-700 px-1.5 py-0.5 rounded">
                          needs customising
                        </span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-gray-600">{t.subjectTemplate}</td>
                    <td className="px-4 py-3 text-gray-400 hidden md:table-cell">
                      {new Date(t.updatedAt).toLocaleDateString("en-GB", {
                        day: "numeric", month: "short", year: "numeric",
                      })}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => openEdit(t)}
                        className="text-brand-600 hover:text-brand-700 text-xs font-medium"
                      >
                        Edit
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {state.view === "edit" && (
        <div className="p-5 space-y-5">
          <div>
            <label className="block text-xs font-medium text-gray-500 uppercase mb-1">
              Course Type (read-only)
            </label>
            <div className="text-sm font-semibold text-gray-900">{state.template.courseType}</div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email Subject
            </label>
            <input
              type="text"
              value={editSubject}
              onChange={(e) => setEditSubject(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
            />
            <p className="text-xs text-gray-400 mt-1">
              You can use <code>{"{{course_dates}}"}</code> and <code>{"{{trainer_name}}"}</code> as variables.
            </p>
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-sm font-medium text-gray-700">
                HTML Body
              </label>
              {/* Source / Preview toggle */}
              <div className="flex rounded-lg border border-gray-200 overflow-hidden">
                <button
                  type="button"
                  onClick={() => setShowPreview(false)}
                  className={`flex items-center gap-1 px-2.5 py-1 text-xs font-medium transition-colors
                    ${!showPreview ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
                >
                  <Code className="w-3 h-3" /> Source
                </button>
                <button
                  type="button"
                  onClick={() => setShowPreview(true)}
                  className={`flex items-center gap-1 px-2.5 py-1 text-xs font-medium border-l border-gray-200 transition-colors
                    ${showPreview ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
                >
                  <Eye className="w-3 h-3" /> Preview
                </button>
              </div>
            </div>

            {!showPreview ? (
              <textarea
                value={editBody}
                onChange={(e) => setEditBody(e.target.value)}
                rows={24}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
                spellCheck={false}
              />
            ) : (
              <div className="border border-gray-300 rounded-lg overflow-auto bg-white" style={{ minHeight: "24rem" }}>
                {editBody.trim() ? (
                  <iframe
                    srcDoc={editBody}
                    className="w-full border-0"
                    style={{ minHeight: "24rem" }}
                    title="Template preview"
                    sandbox="allow-same-origin"
                  />
                ) : (
                  <div className="flex items-center justify-center h-48 text-sm text-gray-400">
                    Nothing to preview — add some HTML in Source mode.
                  </div>
                )}
              </div>
            )}

            <p className="text-xs text-gray-400 mt-1">
              Variables: <code>{"{{greeting}}"}</code>, <code>{"{{trainer_name}}"}</code>,{" "}
              <code>{"{{course_dates}}"}</code>, <code>{"{{delay_note}}"}</code>
            </p>
          </div>

          {saveError && (
            <AlertBanner variant="danger" onDismiss={() => setSaveError("")}>{saveError}</AlertBanner>
          )}

          <div className="flex items-center gap-3 pt-1">
            <Button onClick={handleSave} disabled={saving}>
              <Save className="w-4 h-4" />
              {saving ? "Saving..." : "Save template"}
            </Button>
            <Button variant="secondary" onClick={() => setState({ view: "list" })}>
              Cancel
            </Button>
            {saveSuccess && (
              <span className="text-sm text-green-600 font-medium">Saved!</span>
            )}
          </div>

          <div className="p-3 bg-gray-50 rounded-lg text-xs text-gray-500">
            <strong>Last updated:</strong>{" "}
            {new Date(state.template.updatedAt).toLocaleString("en-GB")}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Pre-Course Templates Editor ───────────────────────────

type PreCourseEditorState =
  | { view: "list" }
  | { view: "edit"; template: PreCourseTemplate };

function PreCourseTemplatesEditor() {
  const [apiKey, setApiKey] = useState<string>("");
  const [templates, setTemplates] = useState<PreCourseTemplate[]>([]);
  const [loading, setLoading] = useState(false);
  const [state, setState] = useState<PreCourseEditorState>({ view: "list" });
  const [editSubject, setEditSubject] = useState("");
  const [editBody, setEditBody] = useState("");
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [showPreview, setShowPreview] = useState(false);

  useEffect(() => {
    setApiKey(localStorage.getItem("bagile_api_key") ?? "");
  }, []);

  const loadTemplates = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);
    try {
      const data = await getPreCourseTemplates(apiKey);
      // Sort by courseType then format
      setTemplates([...data].sort((a, b) =>
        a.courseType.localeCompare(b.courseType) || a.format.localeCompare(b.format)
      ));
    } catch {
      // silently skip — api key may not be set yet
    } finally {
      setLoading(false);
    }
  }, [apiKey]);

  useEffect(() => { loadTemplates(); }, [loadTemplates]);

  function openEdit(template: PreCourseTemplate) {
    setEditSubject(template.subjectTemplate);
    setEditBody(template.htmlBody);
    setSaveError("");
    setSaveSuccess(false);
    setShowPreview(false);
    setState({ view: "edit", template });
  }

  async function handleSave() {
    if (state.view !== "edit") return;
    setSaving(true);
    setSaveError("");
    setSaveSuccess(false);
    try {
      await updatePreCourseTemplate(apiKey, state.template.courseType, {
        format: state.template.format,
        subjectTemplate: editSubject,
        htmlBody: editBody,
      });
      setSaveSuccess(true);
      await loadTemplates();
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (err: any) {
      setSaveError(err.message ?? "Failed to save template");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="px-5 py-3 border-b border-gray-200 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-indigo-50 rounded-lg">
            <FileText className="w-5 h-5 text-indigo-600" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Pre-Course Email Templates</h2>
            <p className="text-sm text-gray-500">Joining details sent before each course type</p>
          </div>
        </div>
        {state.view === "edit" && (
          <button
            onClick={() => setState({ view: "list" })}
            className="flex items-center gap-1 text-sm text-gray-500 hover:text-gray-900"
          >
            <ChevronLeft className="w-4 h-4" /> Back to list
          </button>
        )}
      </div>

      {state.view === "list" && (
        <div>
          {loading && (
            <div className="p-6 text-center text-sm text-gray-400">Loading templates...</div>
          )}
          {!loading && !apiKey && (
            <div className="p-6 text-center text-sm text-gray-400">
              Set your API key on the General tab to manage templates.
            </div>
          )}
          {!loading && apiKey && templates.length === 0 && (
            <EmptyState
              icon={<FileText className="w-10 h-10" />}
              title="No pre-course templates found"
              description="Run the migration to seed pre-course templates, then reload."
            />
          )}
          {!loading && templates.length > 0 && (
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Course Type</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Format</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Subject</th>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Last Updated</th>
                  <th className="px-4 py-2"></th>
                </tr>
              </thead>
              <tbody>
                {templates.map((t) => (
                  <tr key={`${t.courseType}-${t.format}`} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{t.courseType}</td>
                    <td className="px-4 py-3">
                      <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${t.format === "Virtual" ? "bg-blue-50 text-blue-700" : "bg-amber-50 text-amber-700"}`}>
                        {t.format}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-600">{t.subjectTemplate}</td>
                    <td className="px-4 py-3 text-gray-400 hidden md:table-cell">
                      {new Date(t.updatedAt).toLocaleDateString("en-GB", {
                        day: "numeric", month: "short", year: "numeric",
                      })}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => openEdit(t)}
                        className="text-brand-600 hover:text-brand-700 text-xs font-medium"
                      >
                        Edit
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {state.view === "edit" && (
        <div className="p-5 space-y-5">
          <div className="flex gap-6">
            <div>
              <label className="block text-xs font-medium text-gray-500 uppercase mb-1">
                Course Type (read-only)
              </label>
              <div className="text-sm font-semibold text-gray-900">{state.template.courseType}</div>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-500 uppercase mb-1">
                Format (read-only)
              </label>
              <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${state.template.format === "Virtual" ? "bg-blue-50 text-blue-700" : "bg-amber-50 text-amber-700"}`}>
                {state.template.format}
              </span>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              Email Subject
            </label>
            <input
              type="text"
              value={editSubject}
              onChange={(e) => setEditSubject(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
            />
            <p className="text-xs text-gray-400 mt-1">
              Variables: <code>{"{{course_name}}"}</code>, <code>{"{{dates}}"}</code>
            </p>
          </div>

          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-sm font-medium text-gray-700">
                HTML Body
              </label>
              <div className="flex rounded-lg border border-gray-200 overflow-hidden">
                <button
                  type="button"
                  onClick={() => setShowPreview(false)}
                  className={`flex items-center gap-1 px-2.5 py-1 text-xs font-medium transition-colors
                    ${!showPreview ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
                >
                  <Code className="w-3 h-3" /> Source
                </button>
                <button
                  type="button"
                  onClick={() => setShowPreview(true)}
                  className={`flex items-center gap-1 px-2.5 py-1 text-xs font-medium border-l border-gray-200 transition-colors
                    ${showPreview ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
                >
                  <Eye className="w-3 h-3" /> Preview
                </button>
              </div>
            </div>

            {!showPreview ? (
              <textarea
                value={editBody}
                onChange={(e) => setEditBody(e.target.value)}
                rows={24}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
                spellCheck={false}
              />
            ) : (
              <div className="border border-gray-300 rounded-lg overflow-auto bg-white" style={{ minHeight: "24rem" }}>
                {editBody.trim() ? (
                  <iframe
                    srcDoc={editBody}
                    className="w-full border-0"
                    style={{ minHeight: "24rem" }}
                    title="Template preview"
                    sandbox="allow-same-origin"
                  />
                ) : (
                  <div className="flex items-center justify-center h-48 text-sm text-gray-400">
                    Nothing to preview — add some HTML in Source mode.
                  </div>
                )}
              </div>
            )}

            <p className="text-xs text-gray-400 mt-1">
              Variables:{" "}
              <code>{"{{course_name}}"}</code>,{" "}
              <code>{"{{dates}}"}</code>,{" "}
              <code>{"{{times}}"}</code>,{" "}
              <code>{"{{trainer_name}}"}</code>,{" "}
              <code>{"{{venue_address}}"}</code>,{" "}
              <code>{"{{zoom_url}}"}</code>,{" "}
              <code>{"{{zoom_id}}"}</code>,{" "}
              <code>{"{{zoom_passcode}}"}</code>
            </p>
          </div>

          {saveError && (
            <AlertBanner variant="danger" onDismiss={() => setSaveError("")}>{saveError}</AlertBanner>
          )}

          <div className="flex items-center gap-3 pt-1">
            <Button onClick={handleSave} disabled={saving}>
              <Save className="w-4 h-4" />
              {saving ? "Saving..." : "Save template"}
            </Button>
            <Button variant="secondary" onClick={() => setState({ view: "list" })}>
              Cancel
            </Button>
            {saveSuccess && (
              <span className="text-sm text-green-600 font-medium">Saved!</span>
            )}
          </div>

          <div className="p-3 bg-gray-50 rounded-lg text-xs text-gray-500">
            <strong>Last updated:</strong>{" "}
            {new Date(state.template.updatedAt).toLocaleString("en-GB")}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Trainers Editor ───────────────────────────────────────

type TrainerFormState = { name: string; email: string; phone: string };
const emptyTrainerForm = (): TrainerFormState => ({ name: "", email: "", phone: "" });

function TrainersEditor() {
  const [apiKey, setApiKey] = useState("");
  const [trainers, setTrainers] = useState<Trainer[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Inline add form
  const [showAdd, setShowAdd] = useState(false);
  const [addForm, setAddForm] = useState<TrainerFormState>(emptyTrainerForm());
  const [adding, setAdding] = useState(false);

  // Inline edit state: maps trainer id → form values
  const [editingId, setEditingId] = useState<number | null>(null);
  const [editForm, setEditForm] = useState<TrainerFormState>(emptyTrainerForm());
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    setApiKey(localStorage.getItem("bagile_api_key") ?? "");
  }, []);

  const loadTrainers = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);
    try {
      setTrainers(await getTrainers(apiKey));
    } catch {
      // api key may not be set yet
    } finally {
      setLoading(false);
    }
  }, [apiKey]);

  useEffect(() => { loadTrainers(); }, [loadTrainers]);

  function startEdit(t: Trainer) {
    setEditingId(t.id);
    setEditForm({ name: t.name, email: t.email, phone: t.phone ?? "" });
    setError("");
  }

  function cancelEdit() {
    setEditingId(null);
    setError("");
  }

  async function handleSave(id: number) {
    if (!editForm.name.trim() || !editForm.email.trim()) {
      setError("Name and email are required");
      return;
    }
    setSaving(true);
    setError("");
    try {
      await updateTrainer(apiKey, id, {
        name: editForm.name.trim(),
        email: editForm.email.trim(),
        phone: editForm.phone.trim() || undefined,
      });
      setEditingId(null);
      await loadTrainers();
    } catch {
      setError("Failed to save — please try again");
    } finally {
      setSaving(false);
    }
  }

  async function handleAdd() {
    if (!addForm.name.trim() || !addForm.email.trim()) {
      setError("Name and email are required");
      return;
    }
    setAdding(true);
    setError("");
    try {
      await createTrainer(apiKey, {
        name: addForm.name.trim(),
        email: addForm.email.trim(),
        phone: addForm.phone.trim() || undefined,
      });
      setAddForm(emptyTrainerForm());
      setShowAdd(false);
      await loadTrainers();
    } catch {
      setError("Failed to add trainer — email may already be in use");
    } finally {
      setAdding(false);
    }
  }

  async function handleDelete(id: number, name: string) {
    if (!confirm(`Remove ${name} from the trainers list?`)) return;
    try {
      await deleteTrainer(apiKey, id);
      await loadTrainers();
    } catch {
      setError("Failed to remove trainer");
    }
  }

  const inputCls = "border border-gray-300 rounded-lg px-3 py-1.5 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500";

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="px-5 py-3 border-b border-gray-200 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <div className="p-2 bg-green-50 rounded-lg">
            <Users className="w-5 h-5 text-green-600" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Trainers</h2>
            <p className="text-sm text-gray-500">Manage the trainer list used across private courses</p>
          </div>
        </div>
        {apiKey && (
          <Button size="sm" onClick={() => { setShowAdd(true); setError(""); }}>
            <Plus className="w-4 h-4" /> Add Trainer
          </Button>
        )}
      </div>

      {error && (
        <div className="px-5 pt-3">
          <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>
        </div>
      )}

      {loading && (
        <div className="p-6 text-center text-sm text-gray-400">Loading trainers...</div>
      )}
      {!loading && !apiKey && (
        <div className="p-6 text-center text-sm text-gray-400">
          Set your API key on the General tab to manage trainers.
        </div>
      )}

      {!loading && apiKey && (
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Name</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Email</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Phone</th>
              <th className="px-4 py-2 w-24"></th>
            </tr>
          </thead>
          <tbody>
            {trainers.map((t) =>
              editingId === t.id ? (
                <tr key={t.id} className="border-t border-gray-100 bg-blue-50">
                  <td className="px-4 py-2">
                    <input
                      className={inputCls}
                      value={editForm.name}
                      onChange={(e) => setEditForm((f) => ({ ...f, name: e.target.value }))}
                      placeholder="Name"
                    />
                  </td>
                  <td className="px-4 py-2">
                    <input
                      className={inputCls}
                      value={editForm.email}
                      onChange={(e) => setEditForm((f) => ({ ...f, email: e.target.value }))}
                      placeholder="Email"
                    />
                  </td>
                  <td className="px-4 py-2 hidden md:table-cell">
                    <input
                      className={inputCls}
                      value={editForm.phone}
                      onChange={(e) => setEditForm((f) => ({ ...f, phone: e.target.value }))}
                      placeholder="Phone (optional)"
                    />
                  </td>
                  <td className="px-4 py-2 text-right">
                    <div className="flex items-center justify-end gap-2">
                      <button
                        onClick={() => handleSave(t.id)}
                        disabled={saving}
                        title="Save"
                        className="text-green-600 hover:text-green-800 disabled:opacity-50"
                      >
                        <Check className="w-4 h-4" />
                      </button>
                      <button onClick={cancelEdit} title="Cancel" className="text-gray-400 hover:text-gray-600">
                        <X className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ) : (
                <tr key={t.id} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-3 font-medium">{t.name}</td>
                  <td className="px-4 py-3 text-gray-600">{t.email}</td>
                  <td className="px-4 py-3 text-gray-400 hidden md:table-cell">{t.phone ?? "—"}</td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex items-center justify-end gap-3">
                      <button
                        onClick={() => startEdit(t)}
                        title="Edit"
                        className="text-gray-400 hover:text-brand-600"
                      >
                        <Pencil className="w-3.5 h-3.5" />
                      </button>
                      <button
                        onClick={() => handleDelete(t.id, t.name)}
                        title="Remove"
                        className="text-gray-400 hover:text-red-500"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              )
            )}

            {/* Inline add row */}
            {showAdd && (
              <tr className="border-t border-gray-100 bg-green-50">
                <td className="px-4 py-2">
                  <input
                    className={inputCls}
                    value={addForm.name}
                    onChange={(e) => setAddForm((f) => ({ ...f, name: e.target.value }))}
                    placeholder="Name"
                    autoFocus
                  />
                </td>
                <td className="px-4 py-2">
                  <input
                    className={inputCls}
                    value={addForm.email}
                    onChange={(e) => setAddForm((f) => ({ ...f, email: e.target.value }))}
                    placeholder="Email"
                  />
                </td>
                <td className="px-4 py-2 hidden md:table-cell">
                  <input
                    className={inputCls}
                    value={addForm.phone}
                    onChange={(e) => setAddForm((f) => ({ ...f, phone: e.target.value }))}
                    placeholder="Phone (optional)"
                  />
                </td>
                <td className="px-4 py-2 text-right">
                  <div className="flex items-center justify-end gap-2">
                    <button
                      onClick={handleAdd}
                      disabled={adding}
                      title="Add"
                      className="text-green-600 hover:text-green-800 disabled:opacity-50"
                    >
                      <Check className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => { setShowAdd(false); setAddForm(emptyTrainerForm()); setError(""); }}
                      title="Cancel"
                      className="text-gray-400 hover:text-gray-600"
                    >
                      <X className="w-4 h-4" />
                    </button>
                  </div>
                </td>
              </tr>
            )}

            {!showAdd && trainers.length === 0 && (
              <tr>
                <td colSpan={4} className="px-4 py-8 text-center text-sm text-gray-400">
                  No active trainers — run the V41 migration to seed Alex and Chris, then reload.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}

// ── Claude PA Docs ────────────────────────────────────────

const PA_TOOLS = [
  { name: "pa_ping",                     system: "Service",     description: "Health check — confirms the PA service is running" },
  { name: "pa_morning_brief",            system: "Overview",    description: "Aggregates Trello CRM, pending transfers, and at-risk courses into a single briefing" },
  { name: "pa_tasks_list",               system: "Tasks",       description: "Lists open PA inbox tasks from the database" },
  { name: "pa_tasks_complete",           system: "Tasks",       description: "Marks a task as completed by ID" },
  { name: "pa_health_status",            system: "Monitoring",  description: "Shows the last automation run result per automation type" },
  { name: "pa_transfer_fooevent_ticket", system: "WordPress",   description: "Playwright: cancels an old FooEvents ticket and creates a new one for the transferred course" },
  { name: "pa_update_trello_card",       system: "Trello",      description: "Moves a card, adds a comment, and sets or clears a due date via the Trello API" },
  { name: "pa_cancel_course",            system: "WooCommerce", description: "Marks a WooCommerce course product as Sold Out (outofstock) without deleting it" },
  { name: "pa_lookup_xero_invoice",      system: "Xero",        description: "Finds an invoice by number or contact name — auto-refreshes the Xero token" },
  { name: "pa_label_gmail_draft",        system: "Gmail/n8n",   description: "Applies the Employee/Pam label to a draft via the n8n webhook" },
  { name: "pa_create_scrumorg_course",   system: "Scrum.org",   description: "Playwright: copies the latest course listing on scrum.org and updates dates and URL" },
];

const SYSTEM_COLOURS: Record<string, string> = {
  "Service":     "bg-gray-100 text-gray-600",
  "Overview":    "bg-blue-50 text-blue-700",
  "Tasks":       "bg-indigo-50 text-indigo-700",
  "Monitoring":  "bg-amber-50 text-amber-700",
  "WordPress":   "bg-orange-50 text-orange-700",
  "Trello":      "bg-sky-50 text-sky-700",
  "WooCommerce": "bg-purple-50 text-purple-700",
  "Xero":        "bg-green-50 text-green-700",
  "Gmail/n8n":   "bg-red-50 text-red-700",
  "Scrum.org":   "bg-teal-50 text-teal-700",
};

const HTTP_CALLERS = [
  { name: "portal",  role: "admin",  description: "BAgile Portal (this app) — reads and manages tasks" },
  { name: "chatgpt", role: "reader", description: "ChatGPT Custom GPT Action — read-only task access" },
  { name: "n8n",     role: "admin",  description: "n8n automation workflows — full task and webhook access" },
];

function ClaudePaDocs() {
  return (
    <div className="space-y-6">

      {/* What is it */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <div className="flex items-center gap-3 mb-3">
          <div className="p-2 bg-violet-50 rounded-lg">
            <Bot className="w-5 h-5 text-violet-600" />
          </div>
          <div>
            <h2 className="text-lg font-semibold text-gray-900">Claude PA — Personal Assistant Service</h2>
            <p className="text-sm text-gray-500">Internal automation layer between Claude and BAgile&apos;s tools</p>
          </div>
        </div>
        <p className="text-sm text-gray-600 leading-relaxed">
          The PA Service is a TypeScript server that gives Claude (and other callers) a set of pre-built tools for
          routine business operations. Instead of Claude making raw API calls to Trello, Xero, WooCommerce, and
          scrum.org individually, each tool is a single auditable action with proper error handling.
          It runs as an MCP server for Claude Code sessions and exposes the same operations over HTTP for the Portal,
          n8n, and ChatGPT.
        </p>
        <div className="mt-4 grid grid-cols-3 gap-3 text-center">
          <div className="bg-gray-50 rounded-lg p-3">
            <p className="text-2xl font-bold text-gray-900">11</p>
            <p className="text-xs text-gray-500 mt-0.5">MCP tools</p>
          </div>
          <div className="bg-gray-50 rounded-lg p-3">
            <p className="text-2xl font-bold text-gray-900">83</p>
            <p className="text-xs text-gray-500 mt-0.5">automated tests</p>
          </div>
          <div className="bg-gray-50 rounded-lg p-3">
            <p className="text-2xl font-bold text-gray-900">8</p>
            <p className="text-xs text-gray-500 mt-0.5">connected systems</p>
          </div>
        </div>
      </div>

      {/* Tool list */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-5 py-3 border-b border-gray-200 flex items-center gap-3">
          <div className="p-2 bg-violet-50 rounded-lg">
            <Zap className="w-4 h-4 text-violet-600" />
          </div>
          <h2 className="text-sm font-semibold text-gray-900">Available Tools</h2>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Tool name</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">System</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">What it does</th>
            </tr>
          </thead>
          <tbody>
            {PA_TOOLS.map((tool) => (
              <tr key={tool.name} className="border-t border-gray-100 hover:bg-gray-50">
                <td className="px-4 py-2.5 font-mono text-xs text-gray-700 whitespace-nowrap">{tool.name}</td>
                <td className="px-4 py-2.5 hidden md:table-cell">
                  <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${SYSTEM_COLOURS[tool.system] ?? "bg-gray-100 text-gray-600"}`}>
                    {tool.system}
                  </span>
                </td>
                <td className="px-4 py-2.5 text-gray-600 text-xs">{tool.description}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* How to connect */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
        <div className="flex items-center gap-3 mb-4">
          <div className="p-2 bg-violet-50 rounded-lg">
            <Globe className="w-4 h-4 text-violet-600" />
          </div>
          <h2 className="text-sm font-semibold text-gray-900">How to Connect</h2>
        </div>

        <div className="space-y-5">
          <div>
            <p className="text-sm font-medium text-gray-800 mb-1">Claude Code — MCP (already configured for Alex)</p>
            <p className="text-xs text-gray-500 mb-2">
              Registered in <code className="bg-gray-100 px-1 rounded">.mcp.json</code> on Alex&apos;s machine as <code className="bg-gray-100 px-1 rounded">bagile-pa</code>.
              All 11 tools appear automatically in any Claude Code session inside the agent directory. No additional setup needed.
            </p>
          </div>

          <div>
            <p className="text-sm font-medium text-gray-800 mb-1">HTTP API — Portal, n8n, ChatGPT</p>
            <p className="text-xs text-gray-500 mb-2">
              The same operations are available over HTTP on port 3001. All requests require a
              <code className="bg-gray-100 mx-1 px-1 rounded">Authorization: Bearer &lt;key&gt;</code> header.
              The <code className="bg-gray-100 px-1 rounded">/health</code> endpoint is public (no auth required).
            </p>
            <pre className="bg-gray-900 rounded-lg p-4 text-gray-300 text-xs overflow-x-auto leading-relaxed">
{`# List open tasks
GET  https://api.bagile.co.uk:3001/tasks
Authorization: Bearer <your-key>

# Mark a task complete
PATCH https://api.bagile.co.uk:3001/tasks/:id
Authorization: Bearer <your-key>

# Health check (no auth)
GET  https://api.bagile.co.uk:3001/health`}
            </pre>
            <p className="text-xs text-gray-400 mt-2">
              The HTTP server needs to be started on Hetzner before these URLs go live.
              Run <code className="bg-gray-100 px-1 rounded">npm run start:http</code> in <code className="bg-gray-100 px-1 rounded">bagile-pa-service/</code> with a process manager (PM2 or systemd).
            </p>
          </div>
        </div>
      </div>

      {/* API keys & callers */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-5 py-3 border-b border-gray-200 flex items-center gap-3">
          <div className="p-2 bg-violet-50 rounded-lg">
            <Shield className="w-4 h-4 text-violet-600" />
          </div>
          <div>
            <h2 className="text-sm font-semibold text-gray-900">API Keys &amp; Caller Access</h2>
            <p className="text-xs text-gray-500">Keys are stored in <code>.mcp.json → PA_API_KEYS</code> on Alex&apos;s machine. Ask Alex for a key.</p>
          </div>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Caller</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Role</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">What they can do</th>
            </tr>
          </thead>
          <tbody>
            {HTTP_CALLERS.map((c) => (
              <tr key={c.name} className="border-t border-gray-100">
                <td className="px-4 py-3 font-medium text-gray-800 capitalize">{c.name}</td>
                <td className="px-4 py-3">
                  <span className={`text-xs font-medium px-1.5 py-0.5 rounded ${c.role === "admin" ? "bg-green-50 text-green-700" : "bg-blue-50 text-blue-700"}`}>
                    {c.role}
                  </span>
                </td>
                <td className="px-4 py-3 text-xs text-gray-600">{c.description}</td>
              </tr>
            ))}
          </tbody>
        </table>
        <div className="px-5 py-3 bg-gray-50 border-t border-gray-100 text-xs text-gray-500 space-y-0.5">
          <p><strong>admin</strong> — read tasks, mark complete, and trigger automations (when those HTTP routes are added)</p>
          <p><strong>reader</strong> — read tasks and mark complete only; blocked from admin-only routes</p>
        </div>
      </div>

      {/* Outstanding */}
      <div className="bg-amber-50 border border-amber-200 rounded-xl p-5">
        <p className="text-sm font-semibold text-amber-800 mb-2">Before the HTTP API goes live</p>
        <ul className="text-xs text-amber-700 space-y-1 list-disc list-inside">
          <li>Start HTTP server on Hetzner — <code>npm run start:http</code> in <code>bagile-pa-service/</code> via PM2 or systemd</li>
          <li>Push Flyway migrations V69 + V70 to the Hetzner production database (local Docker DB is done)</li>
          <li>Generate OpenAPI spec for ChatGPT Custom GPT Action (once server is live)</li>
          <li>Verify scrum.org Playwright selectors — run <code>pa_create_scrumorg_course</code> once against the live site</li>
        </ul>
      </div>

    </div>
  );
}
