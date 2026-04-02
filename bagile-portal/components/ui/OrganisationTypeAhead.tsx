"use client";

import { useState, useEffect, useRef, useCallback } from "react";
import { X, Building2, Plus, Loader2 } from "lucide-react";
import { searchOrganisations, createOrganisation, OrgSummary } from "@/lib/api";

interface Props {
  apiKey: string;
  value: OrgSummary | null;
  onSelect: (org: OrgSummary | null) => void;
  placeholder?: string;
  disabled?: boolean;
}

/** Derive an acronym suggestion from a multi-word name. e.g. "Frazer-Nash Consultancy Ltd" -> "FNC" */
function suggestAcronym(name: string): string {
  return name
    .split(/[\s\-]+/)
    .filter((w) => w.length > 0 && /[A-Za-z]/.test(w[0]))
    .map((w) => w[0].toUpperCase())
    .join("");
}

export function OrganisationTypeAhead({ apiKey, value, onSelect, placeholder = "Search organisations…", disabled = false }: Props) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<OrgSummary[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  // Create-new inline form state
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState("");
  const [newAcronym, setNewAcronym] = useState("");
  const [saving, setSaving] = useState(false);

  const containerRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Search with 300ms debounce
  const search = useCallback((q: string) => {
    if (debounceRef.current) clearTimeout(debounceRef.current);
    if (!q.trim()) { setResults([]); setOpen(false); return; }
    debounceRef.current = setTimeout(async () => {
      setLoading(true);
      setError("");
      try {
        const items = await searchOrganisations(apiKey, q);
        setResults(items);
        setOpen(true);
      } catch {
        // Graceful degradation: search failure doesn't block the form
        setResults([]);
        setError("Search unavailable — type the org name manually");
      } finally {
        setLoading(false);
      }
    }, 300);
  }, [apiKey]);

  useEffect(() => {
    search(query);
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current); };
  }, [query, search]);

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
        setCreating(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  function handleSelect(org: OrgSummary) {
    onSelect(org);
    setQuery("");
    setResults([]);
    setOpen(false);
    setCreating(false);
    setError("");
  }

  function handleClear() {
    onSelect(null);
    setQuery("");
    setResults([]);
    setOpen(false);
    setCreating(false);
    setTimeout(() => inputRef.current?.focus(), 0);
  }

  function handleStartCreate(typedName: string) {
    setCreating(true);
    setNewName(typedName);
    setNewAcronym(suggestAcronym(typedName));
    setOpen(false);
  }

  async function handleCreate() {
    if (!newName.trim()) return;
    setSaving(true);
    setError("");
    try {
      const org = await createOrganisation(apiKey, newName.trim(), newAcronym.trim() || undefined);
      handleSelect(org);
    } catch {
      setError("Failed to create organisation — name may already exist");
    } finally {
      setSaving(false);
    }
  }

  // If an org is selected, show the chip
  if (value) {
    return (
      <div className="flex items-center gap-2">
        <span className="inline-flex items-center gap-1.5 bg-brand-50 border border-brand-200 text-brand-800 text-sm rounded-lg px-3 py-1.5 font-medium">
          <Building2 className="w-3.5 h-3.5 text-brand-500" />
          {value.name}
          {value.acronym && (
            <span className="ml-0.5 text-brand-500 font-normal text-xs">({value.acronym})</span>
          )}
        </span>
        {!disabled && (
          <button
            type="button"
            onClick={handleClear}
            className="text-gray-400 hover:text-red-500 transition-colors"
            aria-label="Clear organisation"
          >
            <X className="w-4 h-4" />
          </button>
        )}
      </div>
    );
  }

  return (
    <div ref={containerRef} className="relative">
      {/* Search input */}
      {!creating && (
        <div className="relative">
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onFocus={() => { if (results.length > 0) setOpen(true); }}
            placeholder={placeholder}
            disabled={disabled}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500 pr-8"
          />
          {loading && (
            <Loader2 className="absolute right-2.5 top-2.5 w-4 h-4 text-gray-400 animate-spin" />
          )}
        </div>
      )}

      {/* Dropdown */}
      {open && !creating && (
        <div className="absolute z-50 mt-1 w-full bg-white border border-gray-200 rounded-lg shadow-lg overflow-hidden">
          {results.length > 0 && (
            <ul className="max-h-56 overflow-y-auto divide-y divide-gray-100">
              {results.map((org) => (
                <li key={org.id}>
                  <button
                    type="button"
                    onMouseDown={(e) => { e.preventDefault(); handleSelect(org); }}
                    className="w-full text-left px-3 py-2.5 hover:bg-brand-50 flex items-center gap-2.5 group"
                  >
                    <Building2 className="w-4 h-4 text-gray-400 group-hover:text-brand-500 flex-shrink-0" />
                    <span className="flex-1 text-sm text-gray-900">{org.name}</span>
                    {org.acronym && (
                      <span className="text-xs text-gray-400 font-mono">{org.acronym}</span>
                    )}
                    {org.ptnTier && (
                      <span className="text-xs bg-amber-100 text-amber-700 rounded px-1.5 py-0.5 font-medium">
                        {org.ptnTier.toUpperCase()}
                      </span>
                    )}
                  </button>
                </li>
              ))}
            </ul>
          )}
          {/* "Create as new" option — always at bottom when query is non-empty */}
          {query.trim().length > 1 && (
            <button
              type="button"
              onMouseDown={(e) => { e.preventDefault(); handleStartCreate(query.trim()); }}
              className="w-full text-left px-3 py-2.5 hover:bg-green-50 flex items-center gap-2 text-sm text-green-700 font-medium border-t border-gray-100"
            >
              <Plus className="w-4 h-4" />
              Create &ldquo;{query.trim()}&rdquo; as new organisation
            </button>
          )}
          {results.length === 0 && query.trim().length <= 1 && (
            <p className="px-3 py-2.5 text-sm text-gray-400">Type to search…</p>
          )}
        </div>
      )}

      {/* Inline create form */}
      {creating && (
        <div className="border border-green-200 rounded-lg bg-green-50 p-3 space-y-3">
          <p className="text-xs font-semibold text-green-800 uppercase tracking-wide">New Organisation</p>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs font-medium text-green-700 mb-1">Name</label>
              <input
                autoFocus
                type="text"
                value={newName}
                onChange={(e) => {
                  setNewName(e.target.value);
                  setNewAcronym(suggestAcronym(e.target.value));
                }}
                className="w-full border border-green-200 rounded-lg px-2.5 py-1.5 text-sm bg-white focus:ring-2 focus:ring-green-400"
                placeholder="Full organisation name"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-green-700 mb-1">Acronym</label>
              <input
                type="text"
                value={newAcronym}
                onChange={(e) => setNewAcronym(e.target.value.toUpperCase().slice(0, 20))}
                className="w-full border border-green-200 rounded-lg px-2.5 py-1.5 text-sm bg-white font-mono focus:ring-2 focus:ring-green-400"
                placeholder="e.g. FNC"
                maxLength={20}
              />
            </div>
          </div>
          {error && <p className="text-xs text-red-600">{error}</p>}
          <div className="flex gap-2 pt-1">
            <button
              type="button"
              onClick={handleCreate}
              disabled={saving || !newName.trim()}
              className="flex items-center gap-1.5 text-xs font-semibold bg-green-600 hover:bg-green-700 text-white rounded-lg px-3 py-1.5 disabled:opacity-50"
            >
              {saving ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Plus className="w-3.5 h-3.5" />}
              {saving ? "Saving…" : "Create"}
            </button>
            <button
              type="button"
              onClick={() => { setCreating(false); setQuery(""); setError(""); }}
              className="text-xs text-gray-500 hover:text-gray-700 px-2 py-1.5"
            >
              Cancel
            </button>
          </div>
        </div>
      )}

      {error && !creating && (
        <p className="mt-1 text-xs text-amber-600">{error}</p>
      )}
    </div>
  );
}
