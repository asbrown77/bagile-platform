"use client";

import { useState } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { TabBar } from "@/components/ui/TabBar";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { AttendeeInput, AddAttendeesResult, addPrivateAttendees, parseAttendees } from "@/lib/api";
import { UserPlus, Trash2, Upload } from "lucide-react";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  courseId: number;
  onAdded: () => void;
  capacity?: number | null;
  currentAttendeeCount?: number;
}

const emptyAttendee = (): AttendeeInput => ({ firstName: "", lastName: "", email: "" });

export function AddAttendeesPanel({ open, onClose, apiKey, courseId, onAdded, capacity, currentAttendeeCount = 0 }: Props) {
  const [mode, setMode] = useState("paste");
  const [rawText, setRawText] = useState("");
  const [parsed, setParsed] = useState<AttendeeInput[]>([]);
  const [manualRows, setManualRows] = useState<AttendeeInput[]>([emptyAttendee()]);
  const [saving, setSaving] = useState(false);
  const [result, setResult] = useState<AddAttendeesResult | null>(null);
  const [error, setError] = useState("");
  const [capacityWarningAcknowledged, setCapacityWarningAcknowledged] = useState(false);

  async function handleParse() {
    if (!rawText.trim()) return;
    setError("");
    try {
      const items = await parseAttendees(apiKey, rawText);
      setParsed(items);
    } catch {
      setError("Failed to parse text");
    }
  }

  function handleFileUpload(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = async (ev) => {
      const text = ev.target?.result as string;
      setRawText(text);
      try {
        const items = await parseAttendees(apiKey, text);
        setParsed(items);
        setMode("paste");
      } catch {
        setError("Failed to parse file");
      }
    };
    reader.readAsText(file);
  }

  function addManualRow() {
    setManualRows((rows) => [...rows, emptyAttendee()]);
  }

  function updateManualRow(index: number, field: keyof AttendeeInput, value: string) {
    setManualRows((rows) => rows.map((r, i) => i === index ? { ...r, [field]: value } : r));
  }

  function removeManualRow(index: number) {
    setManualRows((rows) => rows.filter((_, i) => i !== index));
  }

  function getAttendees(): AttendeeInput[] {
    if (mode === "paste") return parsed;
    return manualRows.filter((r) => r.email.trim());
  }

  function wouldExceedCapacity(): boolean {
    if (!capacity) return false;
    const attendees = getAttendees();
    return (currentAttendeeCount + attendees.length) > capacity;
  }

  async function handleSubmit() {
    const attendees = getAttendees();
    if (attendees.length === 0) { setError("No attendees to add"); return; }

    if (wouldExceedCapacity() && !capacityWarningAcknowledged) {
      setCapacityWarningAcknowledged(true);
      return;
    }

    setError("");
    setSaving(true);
    try {
      const res = await addPrivateAttendees(apiKey, courseId, attendees);
      setResult(res);
      if (res.created > 0) onAdded();
    } catch {
      setError("Failed to add attendees");
    } finally {
      setSaving(false);
    }
  }

  function handleClose() {
    setRawText("");
    setParsed([]);
    setManualRows([emptyAttendee()]);
    setResult(null);
    setError("");
    setCapacityWarningAcknowledged(false);
    onClose();
  }

  const tabs = [
    { id: "paste", label: "Paste Text" },
    { id: "manual", label: "Manual Entry" },
    { id: "file", label: "Upload File" },
  ];

  return (
    <SlideOver open={open} onClose={handleClose} title="Add Attendees" subtitle="Add students to this course" wide>
      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

      {result ? (
        <div className="space-y-4">
          <AlertBanner variant={result.errors.length > 0 ? "warning" : "success"}>
            <p className="font-medium">{result.created} attendee{result.created !== 1 ? "s" : ""} added</p>
            {result.alreadyEnrolled > 0 && <p className="text-sm mt-1">{result.alreadyEnrolled} already enrolled (skipped)</p>}
            {result.errors.map((err, i) => <p key={i} className="text-sm mt-1">{err}</p>)}
          </AlertBanner>
          <Button onClick={handleClose}>Done</Button>
        </div>
      ) : (
        <>
          <TabBar tabs={tabs} activeTab={mode} onChange={setMode} />

          {/* Paste mode */}
          {mode === "paste" && (
            <div className="space-y-4">
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">
                  Paste attendee list (one per line)
                </label>
                <textarea
                  value={rawText}
                  onChange={(e) => { setRawText(e.target.value); setParsed([]); }}
                  placeholder={"John, Smith, john@example.com\nJane, Doe, jane@example.com, Acme Ltd\n\nOr tab-separated from Excel"}
                  rows={6}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono"
                />
                <p className="text-xs text-gray-400 mt-1">
                  3 columns: <code className="bg-gray-100 px-1 rounded">FirstName, LastName, Email</code>
                  {" "}— or 4 columns to include company:{" "}
                  <code className="bg-gray-100 px-1 rounded">FirstName, LastName, Email, Company</code>
                </p>
                <Button variant="secondary" size="sm" onClick={handleParse} className="mt-2">
                  Parse & Preview
                </Button>
              </div>

              {parsed.length > 0 && (
                <div>
                  <p className="text-xs font-medium text-gray-700 mb-2">{parsed.length} attendee{parsed.length !== 1 ? "s" : ""} found:</p>
                  <div className="bg-gray-50 rounded-lg border border-gray-200 overflow-hidden">
                    <table className="w-full text-xs">
                      <thead className="bg-gray-100">
                        <tr>
                          <th className="text-left px-3 py-2">First</th>
                          <th className="text-left px-3 py-2">Last</th>
                          <th className="text-left px-3 py-2">Email</th>
                          <th className="text-left px-3 py-2">Company</th>
                        </tr>
                      </thead>
                      <tbody>
                        {parsed.map((a, i) => (
                          <tr key={i} className="border-t border-gray-200">
                            <td className="px-3 py-1.5">{a.firstName}</td>
                            <td className="px-3 py-1.5">{a.lastName}</td>
                            <td className="px-3 py-1.5 text-brand-600">{a.email}</td>
                            <td className="px-3 py-1.5 text-gray-400">{a.company || "—"}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                </div>
              )}
            </div>
          )}

          {/* Manual mode */}
          {mode === "manual" && (
            <div className="space-y-3">
              {manualRows.map((row, i) => (
                <div key={i} className="flex gap-2 items-start">
                  <div className="grid grid-cols-3 gap-2 flex-1">
                    <input type="text" placeholder="First name" value={row.firstName}
                      onChange={(e) => updateManualRow(i, "firstName", e.target.value)}
                      className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm" />
                    <input type="text" placeholder="Last name" value={row.lastName}
                      onChange={(e) => updateManualRow(i, "lastName", e.target.value)}
                      className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm" />
                    <input type="email" placeholder="Email" value={row.email}
                      onChange={(e) => updateManualRow(i, "email", e.target.value)}
                      className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm" />
                  </div>
                  {manualRows.length > 1 && (
                    <button onClick={() => removeManualRow(i)} className="text-gray-400 hover:text-red-500 mt-1.5">
                      <Trash2 className="w-4 h-4" />
                    </button>
                  )}
                </div>
              ))}
              <button onClick={addManualRow}
                className="flex items-center gap-1.5 text-sm text-brand-600 hover:text-brand-700 font-medium">
                <UserPlus className="w-4 h-4" /> Add another
              </button>
            </div>
          )}

          {/* File mode */}
          {mode === "file" && (
            <div className="space-y-4">
              <div className="border-2 border-dashed border-gray-300 rounded-xl p-8 text-center">
                <Upload className="w-8 h-8 text-gray-300 mx-auto mb-3" />
                <p className="text-sm text-gray-600 mb-2">Upload a CSV file with attendee details</p>
                <p className="text-xs text-gray-400 mb-4">Format: FirstName, LastName, Email [, Company] — one per line</p>
                <label className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-brand-600 bg-brand-50 rounded-lg hover:bg-brand-100 cursor-pointer">
                  <Upload className="w-4 h-4" /> Choose File
                  <input type="file" accept=".csv,.txt" onChange={handleFileUpload} className="hidden" />
                </label>
              </div>
              {parsed.length > 0 && (
                <AlertBanner variant="success">
                  {parsed.length} attendee{parsed.length !== 1 ? "s" : ""} parsed from file — switch to "Paste Text" tab to review
                </AlertBanner>
              )}
            </div>
          )}

          {/* Submit */}
          {getAttendees().length > 0 && (
            <div className="pt-4 mt-4 border-t border-gray-200 space-y-3">
              {capacityWarningAcknowledged && wouldExceedCapacity() && (
                <AlertBanner variant="warning">
                  This will exceed the course capacity of {capacity}. Currently {currentAttendeeCount} enrolled, adding {getAttendees().length} would bring the total to {currentAttendeeCount + getAttendees().length}.
                </AlertBanner>
              )}
              <div className="flex gap-3">
                <Button onClick={handleSubmit} disabled={saving}>
                  {saving ? "Adding..." : capacityWarningAcknowledged && wouldExceedCapacity()
                    ? `Add anyway (${getAttendees().length})`
                    : `Add ${getAttendees().length} Attendee${getAttendees().length !== 1 ? "s" : ""}`}
                </Button>
                {capacityWarningAcknowledged && wouldExceedCapacity() && (
                  <Button variant="secondary" onClick={() => setCapacityWarningAcknowledged(false)}>Go back</Button>
                )}
                <Button variant="secondary" onClick={handleClose}>Cancel</Button>
              </div>
            </div>
          )}
        </>
      )}
    </SlideOver>
  );
}
