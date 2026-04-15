"use client";
import { useCallback, useEffect, useState } from "react";
import {
  CourseDef, getCourseDefinitions, updateCourseBadgeUrl, updateCourseDuration,
  updateCourseName, getCourseAliases, addCourseAlias, deleteCourseAlias, createCourseDefinition,
} from "@/lib/api";
import { getBadgeSrc, extractCourseTypeFromSku } from "@/lib/calendarHelpers";
import { Button } from "@/components/ui/Button";
import { Plus } from "lucide-react";

// ── Course Definitions Editor ─────────────────────────────

export function CourseDefsEditor() {
  const [apiKey, setApiKey] = useState<string>("");
  const [defs, setDefs] = useState<CourseDef[]>([]);
  const [loading, setLoading] = useState(false);
  const [showAddForm, setShowAddForm] = useState(false);
  const [addCode, setAddCode] = useState("");
  const [addName, setAddName] = useState("");
  const [addDays, setAddDays] = useState(2);
  const [addError, setAddError] = useState("");
  const [addSaving, setAddSaving] = useState(false);

  useEffect(() => {
    const key = localStorage.getItem("bagile_api_key") ?? "";
    setApiKey(key);
  }, []);

  const loadDefs = useCallback(async (key: string) => {
    if (!key) return;
    setLoading(true);
    try {
      const fetched = await getCourseDefinitions(key);
      const withAliases = await Promise.all(
        fetched.map(async (d) => {
          try { return { ...d, aliases: await getCourseAliases(key, d.code) }; }
          catch { return { ...d, aliases: [] }; }
        })
      );
      setDefs(withAliases);
    } catch {
      // silently skip — api key may not be set yet
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => { loadDefs(apiKey); }, [apiKey, loadDefs]);

  async function handleAddSave() {
    setAddError("");
    if (!addCode.trim() || !addName.trim()) { setAddError("Code and name are required"); return; }
    if (addDays < 1 || addDays > 10) { setAddError("Days must be 1–10"); return; }
    setAddSaving(true);
    try {
      await createCourseDefinition(apiKey, addCode.trim(), addName.trim(), addDays);
      setShowAddForm(false);
      setAddCode(""); setAddName(""); setAddDays(2);
      loadDefs(apiKey);
    } catch (err: any) {
      setAddError(err?.message ?? "Failed to create");
    } finally {
      setAddSaving(false);
    }
  }

  if (!apiKey) {
    return (
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-8 text-center max-w-md">
        <p className="text-sm text-gray-500">Set your API key on the General tab to manage course definitions.</p>
      </div>
    );
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="px-5 py-4 border-b border-gray-200 flex items-center justify-between">
        <div>
          <h2 className="text-sm font-semibold text-gray-900">Course Definitions</h2>
          <p className="text-xs text-gray-500 mt-1">Manage course codes, names, durations, badges, and aliases.</p>
        </div>
        <Button size="sm" onClick={() => { setShowAddForm(true); setAddError(""); }}>
          <Plus className="w-3.5 h-3.5 mr-1" /> Add Course
        </Button>
      </div>
      {loading ? (
        <p className="px-5 py-8 text-sm text-gray-400">Loading...</p>
      ) : (
        <table className="w-full text-sm">
          <thead className="bg-gray-50">
            <tr>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase w-14">Badge</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase w-24">Code</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Name / Aliases</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase w-24">Days</th>
              <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase w-20">Status</th>
            </tr>
          </thead>
          <tbody>
            {defs.map((def) => (
              <CourseDefRow key={def.code} def={def} apiKey={apiKey} onRefresh={() => loadDefs(apiKey)} />
            ))}
            {showAddForm && (
              <tr className="border-t border-brand-100 bg-brand-50">
                <td className="px-4 py-2" />
                <td className="px-4 py-2">
                  <input autoFocus value={addCode} onChange={(e) => setAddCode(e.target.value.toUpperCase())}
                    placeholder="CODE" className="w-20 border border-gray-300 rounded px-2 py-1 text-xs font-mono uppercase focus:outline-none focus:border-brand-400" />
                </td>
                <td className="px-4 py-2">
                  <input value={addName} onChange={(e) => setAddName(e.target.value)}
                    placeholder="Course name" className="w-full border border-gray-300 rounded px-2 py-1 text-xs focus:outline-none focus:border-brand-400" />
                  {addError && <p className="text-xs text-red-500 mt-0.5">{addError}</p>}
                </td>
                <td className="px-4 py-2">
                  <input type="number" min={1} max={10} value={addDays} onChange={(e) => setAddDays(Number(e.target.value))}
                    className="w-14 border border-gray-300 rounded px-2 py-1 text-xs text-center focus:outline-none focus:border-brand-400" />
                </td>
                <td className="px-4 py-2">
                  <div className="flex gap-1">
                    <button onClick={handleAddSave} disabled={addSaving}
                      className="text-xs px-2 py-1 rounded bg-brand-600 text-white hover:bg-brand-700 disabled:opacity-50">
                      {addSaving ? "..." : "Save"}
                    </button>
                    <button onClick={() => { setShowAddForm(false); setAddError(""); }}
                      className="text-xs px-2 py-1 rounded bg-gray-100 text-gray-600 hover:bg-gray-200">
                      Cancel
                    </button>
                  </div>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      )}
    </div>
  );
}

// ── Course Def Row ────────────────────────────────────────

function CourseDefRow({ def, apiKey, onRefresh }: { def: CourseDef; apiKey: string; onRefresh: () => void }) {
  const computedBadge = getBadgeSrc(extractCourseTypeFromSku(def.code));
  const [badgeSrc, setBadgeSrc] = useState(def.badgeUrl || computedBadge || "");
  const [badgeSaving, setBadgeSaving] = useState(false);
  const [badgeError, setBadgeError] = useState("");
  const [isDragging, setIsDragging] = useState(false);

  const [duration, setDuration] = useState(def.durationDays);
  const [savedDays, setSavedDays] = useState(def.durationDays);
  const [editingDuration, setEditingDuration] = useState(false);
  const [durationSaving, setDurationSaving] = useState(false);
  const [durationSaved, setDurationSaved] = useState(false);

  const [editingName, setEditingName] = useState(false);
  const [nameVal, setNameVal] = useState(def.name);
  const [nameSaving, setNameSaving] = useState(false);
  const [nameSaved, setNameSaved] = useState(false);

  const [aliases, setAliases] = useState<string[]>(def.aliases ?? []);
  const [addingAlias, setAddingAlias] = useState(false);
  const [newAlias, setNewAlias] = useState("");
  const [aliasError, setAliasError] = useState("");

  function handleFileRead(file: File) {
    if (!file.type.startsWith("image/")) { setBadgeError("Please drop an image file"); return; }
    const reader = new FileReader();
    reader.onload = async (e) => {
      const dataUrl = e.target?.result as string;
      setBadgeSaving(true); setBadgeError("");
      try {
        await updateCourseBadgeUrl(apiKey, def.code, dataUrl);
        setBadgeSrc(dataUrl);
      } catch (err: any) {
        setBadgeError(err?.message ?? "Failed to save badge");
      } finally { setBadgeSaving(false); }
    };
    reader.readAsDataURL(file);
  }

  async function handleDurationSave(val: number) {
    if (val === savedDays || val < 1 || val > 10) { setEditingDuration(false); setDuration(savedDays); return; }
    setDurationSaving(true);
    try {
      await updateCourseDuration(apiKey, def.code, val);
      setSavedDays(val);
      setDurationSaved(true);
      setTimeout(() => setDurationSaved(false), 2000);
    } finally { setDurationSaving(false); setEditingDuration(false); }
  }

  async function handleNameSave() {
    const trimmed = nameVal.trim();
    if (!trimmed || trimmed === def.name) { setEditingName(false); setNameVal(def.name); return; }
    setNameSaving(true);
    try {
      await updateCourseName(apiKey, def.code, trimmed);
      setNameSaved(true);
      setTimeout(() => setNameSaved(false), 2000);
      setEditingName(false);
    } catch { setNameVal(def.name); setEditingName(false); }
    finally { setNameSaving(false); }
  }

  async function handleAddAlias() {
    const trimmed = newAlias.trim();
    if (!trimmed) return;
    setAliasError("");
    try {
      await addCourseAlias(apiKey, def.code, trimmed);
      setAliases((prev) => [...prev, trimmed].sort());
      setNewAlias(""); setAddingAlias(false);
    } catch (err: any) {
      setAliasError(err?.message ?? "Failed to add alias");
    }
  }

  async function handleRemoveAlias(alias: string) {
    try {
      await deleteCourseAlias(apiKey, def.code, alias);
      setAliases((prev) => prev.filter((a) => a !== alias));
    } catch { /* ignore */ }
  }

  return (
    <tr className="border-t border-gray-100 align-top">
      {/* Badge */}
      <td className="px-4 py-3">
        <label
          className={`relative flex items-center justify-center h-12 w-12 rounded-lg border-2 cursor-pointer transition-colors
            ${isDragging ? "border-brand-400 bg-brand-50" : "border-dashed border-gray-300 hover:border-brand-400 hover:bg-gray-50"}`}
          title="Drop or click to upload a badge image"
          onDragOver={(e) => { e.preventDefault(); setIsDragging(true); }}
          onDragLeave={() => setIsDragging(false)}
          onDrop={(e) => { e.preventDefault(); setIsDragging(false); const f = e.dataTransfer.files[0]; if (f) handleFileRead(f); }}
        >
          {badgeSaving ? <span className="text-xs text-gray-400">...</span>
            : badgeSrc ? <img src={badgeSrc} alt={def.code} className="h-9 w-9 object-contain" />
            : <span className="text-xs text-gray-400">+</span>}
          <input type="file" accept="image/*" className="sr-only"
            onChange={(e) => { const f = e.target.files?.[0]; if (f) handleFileRead(f); }} />
        </label>
        {badgeError && <p className="text-xs text-red-500 mt-0.5 w-12">{badgeError}</p>}
      </td>
      {/* Code */}
      <td className="px-4 py-3 font-semibold text-gray-900 align-top pt-4">{def.code}</td>
      {/* Name + aliases */}
      <td className="px-4 py-3">
        {editingName ? (
          <input autoFocus value={nameVal} onChange={(e) => setNameVal(e.target.value)}
            onBlur={handleNameSave}
            onKeyDown={(e) => { if (e.key === "Enter") handleNameSave(); if (e.key === "Escape") { setEditingName(false); setNameVal(def.name); } }}
            disabled={nameSaving}
            className="w-full border border-brand-400 rounded px-2 py-1 text-sm focus:outline-none" />
        ) : (
          <button onClick={() => setEditingName(true)}
            className="text-sm text-gray-800 hover:text-brand-600 text-left"
            title="Click to edit name">
            {nameSaved ? <span className="text-green-600">✓ {nameVal}</span> : nameVal}
          </button>
        )}
        {/* Aliases */}
        <div className="flex flex-wrap gap-1 mt-1.5 items-center">
          {aliases.map((a) => (
            <span key={a} className="inline-flex items-center gap-0.5 px-1.5 py-0.5 rounded text-xs bg-gray-100 text-gray-600">
              {a}
              <button onClick={() => handleRemoveAlias(a)} className="text-gray-400 hover:text-red-500 leading-none" title="Remove alias">×</button>
            </span>
          ))}
          {addingAlias ? (
            <span className="inline-flex items-center gap-1">
              <input autoFocus value={newAlias} onChange={(e) => setNewAlias(e.target.value)}
                onKeyDown={(e) => { if (e.key === "Enter") handleAddAlias(); if (e.key === "Escape") { setAddingAlias(false); setNewAlias(""); setAliasError(""); } }}
                className="w-24 border border-gray-300 rounded px-1.5 py-0.5 text-xs focus:outline-none focus:border-brand-400" placeholder="alias" />
              <button onClick={handleAddAlias} className="text-xs text-brand-600 hover:underline">Add</button>
              <button onClick={() => { setAddingAlias(false); setNewAlias(""); setAliasError(""); }} className="text-xs text-gray-400 hover:text-gray-600">Cancel</button>
            </span>
          ) : (
            <button onClick={() => setAddingAlias(true)} className="text-xs text-gray-400 hover:text-brand-500" title="Add alias">+ alias</button>
          )}
        </div>
        {aliasError && <p className="text-xs text-red-500 mt-0.5">{aliasError}</p>}
      </td>
      {/* Duration */}
      <td className="px-4 py-3 align-top pt-4">
        {editingDuration ? (
          <input type="number" min={1} max={10} autoFocus value={duration}
            onChange={(e) => setDuration(Number(e.target.value))}
            onBlur={() => handleDurationSave(duration)}
            onKeyDown={(e) => { if (e.key === "Enter") handleDurationSave(duration); if (e.key === "Escape") { setEditingDuration(false); setDuration(savedDays); } }}
            className="w-14 border border-brand-400 rounded px-2 py-1 text-sm text-center focus:outline-none"
            disabled={durationSaving} />
        ) : (
          <button onClick={() => setEditingDuration(true)}
            className="text-sm text-gray-700 hover:text-brand-600 hover:underline tabular-nums"
            title="Click to edit duration">
            {durationSaved ? <span className="text-green-600">✓ {duration}d</span> : `${duration}d`}
          </button>
        )}
      </td>
      {/* Status */}
      <td className="px-4 py-3 align-top pt-4">
        {def.active
          ? <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-700">Active</span>
          : <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-gray-100 text-gray-500">Inactive</span>}
      </td>
    </tr>
  );
}
