"use client";

/**
 * CSV Import Modal — Sprint 28 Item 7.
 * Upload a CSV of planned courses → preview table → confirm → bulk create.
 *
 * CSV format: courseType,startDate,endDate,trainer,isVirtual,venue,notes
 * Example:    PSM,2026-06-02,2026-06-03,Alex Brown,true,,
 */

import { useState, useRef } from "react";
import { X, Upload, Loader2, AlertCircle, CheckCircle2 } from "lucide-react";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { Badge } from "@/components/ui/Badge";
import { Trainer, BulkCourseRow, BulkCreateResult, bulkCreatePlannedCourses } from "@/lib/api";
import { getCourseCodeDisplay } from "@/lib/calendarHelpers";

// ── Types ──────────────────────────────────────────────────

interface ParsedRow {
  line: number;
  courseType: string;
  startDate: string;
  endDate: string;
  trainerName: string;
  isVirtual: boolean;
  venue: string;
  notes: string;
  error?: string;
}

interface CsvImportModalProps {
  open: boolean;
  onClose: () => void;
  onImported: () => void;
  trainers: Trainer[];
  apiKey: string;
}

// ── CSV parsing ────────────────────────────────────────────

function parseRow(raw: string, lineNum: number): ParsedRow {
  // Handle quoted fields
  const cols = raw.split(",").map((c) => c.trim().replace(/^"|"$/g, ""));
  const [courseType = "", startDate = "", endDate = "", trainerName = "", isVirtualStr = "true", venue = "", notes = ""] = cols;
  const isVirtual = isVirtualStr.toLowerCase() !== "false";

  const errors: string[] = [];
  if (!courseType) errors.push("courseType required");
  if (!startDate || !/^\d{4}-\d{2}-\d{2}$/.test(startDate)) errors.push("startDate must be YYYY-MM-DD");
  if (!endDate || !/^\d{4}-\d{2}-\d{2}$/.test(endDate)) errors.push("endDate must be YYYY-MM-DD");
  if (endDate && startDate && endDate < startDate) errors.push("endDate before startDate");

  return {
    line: lineNum,
    courseType: courseType.toUpperCase().replace(/[-_\s]/g, ""),
    startDate,
    endDate,
    trainerName,
    isVirtual,
    venue,
    notes,
    error: errors.length > 0 ? errors.join("; ") : undefined,
  };
}

function parseCsv(text: string): ParsedRow[] {
  const lines = text.split(/\r?\n/).map((l) => l.trim()).filter(Boolean);
  const dataLines = lines[0]?.toLowerCase().includes("coursetype") ? lines.slice(1) : lines;
  return dataLines.map((line, i) => parseRow(line, i + 1));
}

// ── Component ──────────────────────────────────────────────

export function CsvImportModal({ open, onClose, onImported, trainers, apiKey }: CsvImportModalProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [parsed, setParsed] = useState<ParsedRow[]>([]);
  const [importing, setImporting] = useState(false);
  const [result, setResult] = useState<BulkCreateResult | null>(null);
  const [fileError, setFileError] = useState("");

  function reset() {
    setParsed([]);
    setResult(null);
    setFileError("");
    if (inputRef.current) inputRef.current.value = "";
  }

  function handleClose() {
    reset();
    onClose();
  }

  function handleFile(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    if (!file.name.endsWith(".csv")) {
      setFileError("Please upload a .csv file");
      return;
    }
    setFileError("");
    const reader = new FileReader();
    reader.onload = (ev) => {
      const text = ev.target?.result as string;
      setParsed(parseCsv(text));
      setResult(null);
    };
    reader.readAsText(file);
  }

  function resolveTrainerId(name: string): number {
    const match = trainers.find((t) => t.name.toLowerCase() === name.toLowerCase());
    return match?.id ?? trainers[0]?.id ?? 0;
  }

  async function handleConfirm() {
    const validRows = parsed.filter((r) => !r.error);
    if (validRows.length === 0) return;
    setImporting(true);
    try {
      const courses: BulkCourseRow[] = validRows.map((r) => ({
        courseType: r.courseType,
        startDate: r.startDate,
        endDate: r.endDate,
        trainerId: resolveTrainerId(r.trainerName),
        isVirtual: r.isVirtual,
        venue: r.venue || undefined,
        notes: r.notes || undefined,
      }));
      const res = await bulkCreatePlannedCourses(apiKey, courses);
      setResult(res);
      if (res.successCount > 0) onImported();
    } finally {
      setImporting(false);
    }
  }

  if (!open) return null;

  const validCount = parsed.filter((r) => !r.error).length;
  const invalidCount = parsed.filter((r) => !!r.error).length;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="fixed inset-0 bg-black/40" onClick={handleClose} />
      <div className="relative bg-white rounded-xl shadow-xl w-full max-w-3xl mx-4 max-h-[90vh] flex flex-col">

        {/* Header */}
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200 shrink-0">
          <h2 className="text-lg font-semibold text-gray-900">Import Planned Courses (CSV)</h2>
          <button onClick={handleClose} className="text-gray-400 hover:text-gray-600 p-1 rounded-lg hover:bg-gray-100">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="flex-1 overflow-y-auto px-6 py-4 space-y-4">

          {/* Format hint */}
          <div className="text-xs text-gray-500 bg-gray-50 rounded-lg px-3 py-2 font-mono">
            courseType,startDate,endDate,trainer,isVirtual,venue,notes<br />
            PSM,2026-06-02,2026-06-03,Alex Brown,true,,
          </div>

          {/* File picker */}
          {!result && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Upload CSV</label>
              <input
                ref={inputRef}
                type="file"
                accept=".csv"
                onChange={handleFile}
                className="block w-full text-sm text-gray-500 file:mr-3 file:py-1.5 file:px-3 file:rounded-lg file:border-0 file:text-sm file:font-medium file:bg-brand-50 file:text-brand-700 hover:file:bg-brand-100"
              />
              {fileError && <p className="text-xs text-red-500 mt-1">{fileError}</p>}
            </div>
          )}

          {/* Parse results / import result */}
          {result ? (
            <ImportResult result={result} parsed={parsed} />
          ) : parsed.length > 0 ? (
            <PreviewTable rows={parsed} />
          ) : null}
        </div>

        {/* Footer */}
        {!result && parsed.length > 0 && (
          <div className="flex items-center justify-between px-6 py-4 border-t border-gray-100 shrink-0">
            <span className="text-sm text-gray-500">
              {validCount} valid, {invalidCount} invalid
            </span>
            <div className="flex gap-2">
              <Button variant="secondary" onClick={reset}>Clear</Button>
              <Button onClick={handleConfirm} disabled={importing || validCount === 0}>
                {importing ? <><Loader2 className="w-4 h-4 animate-spin" /> Importing...</> : `Import ${validCount} course${validCount !== 1 ? "s" : ""}`}
              </Button>
            </div>
          </div>
        )}

        {result && (
          <div className="flex justify-end px-6 py-4 border-t border-gray-100 shrink-0">
            <Button onClick={handleClose}>Done</Button>
          </div>
        )}
      </div>
    </div>
  );
}

// ── Preview table ──────────────────────────────────────────

function PreviewTable({ rows }: { rows: ParsedRow[] }) {
  const fmt = (d: string) => d || "—";
  return (
    <div className="overflow-x-auto rounded-lg border border-gray-200">
      <table className="w-full text-xs">
        <thead>
          <tr className="bg-gray-50 border-b border-gray-200">
            <th className="px-3 py-2 text-left font-semibold text-gray-500">#</th>
            <th className="px-3 py-2 text-left font-semibold text-gray-500">Course</th>
            <th className="px-3 py-2 text-left font-semibold text-gray-500">Start</th>
            <th className="px-3 py-2 text-left font-semibold text-gray-500">End</th>
            <th className="px-3 py-2 text-left font-semibold text-gray-500">Trainer</th>
            <th className="px-3 py-2 text-left font-semibold text-gray-500">Format</th>
            <th className="px-3 py-2 text-left font-semibold text-gray-500">Status</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((r) => (
            <tr key={r.line} className={`border-b border-gray-100 ${r.error ? "bg-red-50" : ""}`}>
              <td className="px-3 py-2 text-gray-400">{r.line}</td>
              <td className="px-3 py-2 font-medium text-gray-800">{getCourseCodeDisplay(r.courseType) || r.courseType}</td>
              <td className="px-3 py-2 text-gray-600">{fmt(r.startDate)}</td>
              <td className="px-3 py-2 text-gray-600">{fmt(r.endDate)}</td>
              <td className="px-3 py-2 text-gray-600">{r.trainerName || "—"}</td>
              <td className="px-3 py-2 text-gray-500">{r.isVirtual ? "Virtual" : "Onsite"}</td>
              <td className="px-3 py-2">
                {r.error ? (
                  <span className="flex items-center gap-1 text-red-600">
                    <AlertCircle className="w-3 h-3" /> {r.error}
                  </span>
                ) : (
                  <Badge variant="success">OK</Badge>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

// ── Import result ──────────────────────────────────────────

function ImportResult({ result, parsed }: { result: BulkCreateResult; parsed: ParsedRow[] }) {
  return (
    <div className="space-y-3">
      <div className="flex items-center gap-4">
        <span className="flex items-center gap-1.5 text-sm text-green-700 font-medium">
          <CheckCircle2 className="w-4 h-4" /> {result.successCount} imported
        </span>
        {result.failureCount > 0 && (
          <span className="flex items-center gap-1.5 text-sm text-red-600 font-medium">
            <AlertCircle className="w-4 h-4" /> {result.failureCount} failed
          </span>
        )}
      </div>
      {result.failureCount > 0 && (
        <div className="space-y-1">
          {result.results.filter((r) => !r.success).map((r) => {
            const row = parsed[r.index];
            return (
              <div key={r.index} className="text-xs text-red-600 bg-red-50 rounded px-3 py-1.5">
                Row {r.index + 1} ({row?.courseType ?? "?"}): {r.error}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
