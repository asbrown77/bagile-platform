"use client";

import { useCallback, useEffect, useState, useRef } from "react";
import { ApiKey, CreateKeyResponse, loginWithGoogle, listKeys, createKey, revokeKey, PostCourseTemplate, listPostCourseTemplates, upsertPostCourseTemplate, Trainer, getTrainers, createTrainer, updateTrainer, deleteTrainer } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { Key, Plus, Settings2, FileText, ChevronLeft, Save, Eye, Code, Users, Pencil, Trash2, X, Check } from "lucide-react";
import { loadConfig, saveConfig, type PortalConfig } from "@/lib/config";

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
      // Auto-save as the dashboard API key
      localStorage.setItem("bagile_api_key", result.key);
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
        <PageHeader title="Settings" subtitle="Sign in to manage your API keys" />
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 text-center max-w-md mx-auto mb-6">
          <Key className="w-10 h-10 text-gray-300 mx-auto mb-4" />
          <h2 className="text-lg font-semibold text-gray-900 mb-2">Create MCP API Keys</h2>
          <p className="text-gray-500 text-sm mb-6">Sign in with Google to manage your API keys.</p>
          <div ref={btnRef} className="flex justify-center" />
          {error && <p className="mt-4 text-red-600 text-sm">{error}</p>}
        </div>
        {/* Course thresholds, templates, and trainers are accessible without Google auth */}
        <CourseThresholds />
        <PostCourseTemplatesEditor />
        <TrainersEditor />
      </>
    );
  }

  return (
    <>
      <PageHeader title="Settings" subtitle={user?.email || ""} />

      {error && <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>}

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

      {/* Course Risk Thresholds */}
      <CourseThresholds />

      {/* Post-Course Email Templates */}
      <PostCourseTemplatesEditor />

      {/* Trainers */}
      <TrainersEditor />
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
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-6">
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
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden mt-6">
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
              Set your API key above to manage templates.
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
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden mt-6">
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
          Set your API key above to manage trainers.
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
