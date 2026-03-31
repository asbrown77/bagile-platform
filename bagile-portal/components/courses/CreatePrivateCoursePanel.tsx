"use client";

import { useState, useEffect } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { createPrivateCourse, addPrivateAttendees, getScheduleConflicts, CreatePrivateCourseRequest, AttendeeInput, ScheduleConflict } from "@/lib/api";
import { Trash2, UserPlus, Copy, FileJson } from "lucide-react";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  onCreated: () => void;
}

const COURSE_CODES = ["PSM", "PSPO", "PSPOAI", "PSPOA", "PAL", "PALE", "PSK", "PSMA", "PSMAI", "PSFS", "APS", "EBM"];

const JSON_TEMPLATE = `{
  "name": "PSM - Company Name",
  "courseCode": "PSM",
  "startDate": "2026-04-15",
  "endDate": "2026-04-16",
  "formatType": "virtual",
  "trainerName": "Alex Brown",
  "capacity": 20,
  "price": 5000,
  "invoiceReference": "INV-00123",
  "meetingUrl": "https://zoom.us/j/123456789",
  "meetingId": "123 456 789",
  "meetingPasscode": "abc123",
  "venueAddress": "",
  "notes": "",
  "attendees": [
    { "firstName": "John", "lastName": "Smith", "email": "john@company.com" },
    { "firstName": "Jane", "lastName": "Doe", "email": "jane@company.com" }
  ]
}`;

const emptyAttendee = (): AttendeeInput => ({ firstName: "", lastName: "", email: "" });

export function CreatePrivateCoursePanel({ open, onClose, apiKey, onCreated }: Props) {
  const [mode, setMode] = useState("form");
  const [form, setForm] = useState<CreatePrivateCourseRequest>({
    name: "", courseCode: "PSM", startDate: "", endDate: "", formatType: "virtual",
  });
  const [attendees, setAttendees] = useState<AttendeeInput[]>([]);
  const [jsonText, setJsonText] = useState("");
  const [jsonParsed, setJsonParsed] = useState<{ course: CreatePrivateCourseRequest; attendees: AttendeeInput[] } | null>(null);
  const [conflicts, setConflicts] = useState<ScheduleConflict[]>([]);

  // Check for conflicts when dates change
  useEffect(() => {
    if (!apiKey || !form.startDate || !form.endDate) { setConflicts([]); return; }
    const timer = setTimeout(() => {
      getScheduleConflicts(apiKey, form.startDate, form.endDate, form.trainerName)
        .then(setConflicts)
        .catch(() => setConflicts([]));
    }, 500);
    return () => clearTimeout(timer);
  }, [apiKey, form.startDate, form.endDate, form.trainerName]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [copied, setCopied] = useState(false);

  function update(field: string, value: string | number | undefined) {
    setForm((f) => ({ ...f, [field]: value }));
  }

  function addAttendeeRow() {
    setAttendees((a) => [...a, emptyAttendee()]);
  }

  function updateAttendee(index: number, field: keyof AttendeeInput, value: string) {
    setAttendees((rows) => rows.map((r, i) => i === index ? { ...r, [field]: value } : r));
  }

  function removeAttendee(index: number) {
    setAttendees((rows) => rows.filter((_, i) => i !== index));
  }

  function parseJson() {
    try {
      const data = JSON.parse(jsonText);
      const course: CreatePrivateCourseRequest = {
        name: data.name || "",
        courseCode: data.courseCode || "PSM",
        startDate: data.startDate || "",
        endDate: data.endDate || "",
        formatType: data.formatType || "virtual",
        trainerName: data.trainerName,
        capacity: data.capacity,
        price: data.price,
        invoiceReference: data.invoiceReference,
        meetingUrl: data.meetingUrl,
        meetingId: data.meetingId,
        meetingPasscode: data.meetingPasscode,
        venueAddress: data.venueAddress,
        notes: data.notes,
      };
      const atts: AttendeeInput[] = (data.attendees || []).map((a: any) => ({
        firstName: a.firstName || "", lastName: a.lastName || "", email: a.email || "",
        company: a.company, country: a.country,
      }));
      setJsonParsed({ course, attendees: atts });
      setError("");
    } catch {
      setError("Invalid JSON — check the format and try again");
      setJsonParsed(null);
    }
  }

  async function handleSubmit(courseData: CreatePrivateCourseRequest, attendeeList: AttendeeInput[]) {
    if (!courseData.name || !courseData.startDate || !courseData.endDate) {
      setError("Name, start date, and end date are required");
      return;
    }
    setError("");
    setSaving(true);
    try {
      const created = await createPrivateCourse(apiKey, courseData);

      // Add attendees if any
      if (attendeeList.length > 0) {
        const validAttendees = attendeeList.filter((a) => a.email.trim());
        if (validAttendees.length > 0) {
          await addPrivateAttendees(apiKey, created.id, validAttendees);
        }
      }

      setForm({ name: "", courseCode: "PSM", startDate: "", endDate: "", formatType: "virtual" });
      setAttendees([]);
      setJsonText("");
      setJsonParsed(null);
      onCreated();
      onClose();
      window.location.href = `/courses/${created.id}`;
    } catch {
      setError("Failed to create course");
    } finally {
      setSaving(false);
    }
  }

  function copyTemplate() {
    navigator.clipboard.writeText(JSON_TEMPLATE);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  const isVirtual = form.formatType === "virtual";

  return (
    <SlideOver open={open} onClose={onClose} title="Create Private Course" subtitle="Add a new private/corporate course" wide>
      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

      {/* ── JSON Import Mode (advanced) ────────────────── */}
      {mode !== "json" && (
        <div className="mb-4">
          <button onClick={() => setMode("json")}
            className="text-xs text-gray-400 hover:text-brand-600 underline">
            Advanced: Import from JSON
          </button>
        </div>
      )}
      {mode === "json" && (
        <div className="space-y-4">
          {/* Template */}
          <div className="bg-gray-50 rounded-lg p-4">
            <div className="flex items-center justify-between mb-2">
              <p className="text-xs font-semibold text-gray-700 uppercase tracking-wide">JSON Template</p>
              <button onClick={copyTemplate}
                className="flex items-center gap-1.5 text-xs text-brand-600 hover:text-brand-700 font-medium">
                <Copy className="w-3.5 h-3.5" />
                {copied ? "Copied!" : "Copy Template"}
              </button>
            </div>
            <p className="text-xs text-gray-500 mb-2">
              Copy this template, give it to Claude or any AI with the client's details, then paste the result below.
            </p>
            <pre className="bg-gray-900 text-gray-300 rounded-lg p-3 text-xs overflow-x-auto max-h-40 overflow-y-auto">
              {JSON_TEMPLATE}
            </pre>
          </div>

          {/* Paste area */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Paste JSON</label>
            <textarea
              value={jsonText}
              onChange={(e) => { setJsonText(e.target.value); setJsonParsed(null); }}
              placeholder="Paste the filled-in JSON here..."
              rows={8}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono"
            />
            <Button variant="secondary" size="sm" onClick={parseJson} className="mt-2">
              <FileJson className="w-3.5 h-3.5" /> Parse & Preview
            </Button>
          </div>

          {/* Preview */}
          {jsonParsed && (
            <div className="bg-green-50 rounded-lg p-4 space-y-3">
              <p className="text-xs font-semibold text-green-800 uppercase tracking-wide">Preview</p>
              <div className="text-sm text-green-900 space-y-1">
                <p><strong>{jsonParsed.course.name}</strong></p>
                <p>{jsonParsed.course.courseCode} — {jsonParsed.course.formatType} — {jsonParsed.course.startDate} to {jsonParsed.course.endDate}</p>
                {jsonParsed.course.trainerName && <p>Trainer: {jsonParsed.course.trainerName}</p>}
                {jsonParsed.course.invoiceReference && <p>Invoice: {jsonParsed.course.invoiceReference}</p>}
                {jsonParsed.attendees.length > 0 && (
                  <div className="mt-2">
                    <p className="font-medium">{jsonParsed.attendees.length} attendees:</p>
                    <ul className="ml-4 text-xs space-y-0.5">
                      {jsonParsed.attendees.map((a, i) => (
                        <li key={i}>{a.firstName} {a.lastName} — {a.email}</li>
                      ))}
                    </ul>
                  </div>
                )}
              </div>
              <div className="flex gap-3 pt-3 border-t border-green-200">
                <Button onClick={() => handleSubmit(jsonParsed.course, jsonParsed.attendees)} disabled={saving}>
                  {saving ? "Creating..." : `Create Course${jsonParsed.attendees.length > 0 ? ` + ${jsonParsed.attendees.length} Attendees` : ""}`}
                </Button>
              </div>
            </div>
          )}
          <button onClick={() => setMode("form")} className="text-xs text-gray-400 hover:text-brand-600 underline mt-4">
            Back to form
          </button>
        </div>
      )}

      {/* ── Form Mode ───────────────────────────────────── */}
      {mode === "form" && (
        <form onSubmit={(e) => { e.preventDefault(); handleSubmit(form, attendees); }} className="space-y-5">
          {/* Course basics */}
          <div className="grid grid-cols-2 gap-4">
            <div className="col-span-2">
              <label className="block text-xs font-medium text-gray-700 mb-1">Course Name</label>
              <input type="text" value={form.name} onChange={(e) => update("name", e.target.value)}
                placeholder="e.g. PSM - Acme Corp" required
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Course Type</label>
              <select value={form.courseCode} onChange={(e) => update("courseCode", e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white">
                {COURSE_CODES.map((c) => <option key={c} value={c}>{c}</option>)}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Format</label>
              <select value={form.formatType} onChange={(e) => update("formatType", e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white">
                <option value="virtual">Virtual</option>
                <option value="in_person">In-person</option>
              </select>
            </div>
          </div>

          {/* Dates */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Start Date</label>
              <input type="date" value={form.startDate} onChange={(e) => update("startDate", e.target.value)} required
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">End Date</label>
              <input type="date" value={form.endDate} onChange={(e) => update("endDate", e.target.value)} required
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
          </div>

          {/* Conflict warning */}
          {conflicts.length > 0 && (
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-3">
              <p className="text-xs font-semibold text-amber-800 mb-1.5">
                {conflicts.length} course{conflicts.length !== 1 ? "s" : ""} overlap these dates:
              </p>
              <ul className="space-y-1">
                {conflicts.slice(0, 5).map((c) => (
                  <li key={c.conflictingCourseId} className="text-xs text-amber-700 flex items-center gap-2">
                    <span className={`w-2 h-2 rounded-full ${c.conflictType === "trainer_clash" ? "bg-red-500" : "bg-amber-400"}`} />
                    <span className="font-medium">{c.courseCode}</span>
                    <span className="text-amber-500">{c.courseName}</span>
                    {c.trainerName && <span>({c.trainerName})</span>}
                    <span>{c.enrolmentCount} enrolled</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {/* Trainer + capacity + price */}
          <div className="grid grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Trainer</label>
              <input type="text" value={form.trainerName || ""} onChange={(e) => update("trainerName", e.target.value)}
                placeholder="Alex Brown"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Capacity</label>
              <input type="number" value={form.capacity || ""} onChange={(e) => update("capacity", e.target.value ? Number(e.target.value) : undefined)}
                placeholder="20"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Price (total)</label>
              <input type="number" value={form.price || ""} onChange={(e) => update("price", e.target.value ? Number(e.target.value) : undefined)}
                placeholder="5000"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
          </div>

          {/* Invoice reference */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Invoice Reference (Xero)</label>
            <input type="text" value={form.invoiceReference || ""} onChange={(e) => update("invoiceReference", e.target.value)}
              placeholder="INV-00123"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>

          {/* Virtual: meeting details */}
          {isVirtual && (
            <div className="bg-blue-50 rounded-lg p-4 space-y-3">
              <p className="text-xs font-semibold text-blue-800 uppercase tracking-wide">Meeting Details</p>
              <div>
                <label className="block text-xs font-medium text-blue-700 mb-1">Zoom/Teams URL</label>
                <input type="url" value={form.meetingUrl || ""} onChange={(e) => update("meetingUrl", e.target.value)}
                  placeholder="https://zoom.us/j/..."
                  className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-blue-700 mb-1">Meeting ID</label>
                  <input type="text" value={form.meetingId || ""} onChange={(e) => update("meetingId", e.target.value)}
                    placeholder="123 456 7890"
                    className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-blue-700 mb-1">Passcode</label>
                  <input type="text" value={form.meetingPasscode || ""} onChange={(e) => update("meetingPasscode", e.target.value)}
                    placeholder="abc123"
                    className="w-full border border-blue-200 rounded-lg px-3 py-2 text-sm bg-white" />
                </div>
              </div>
            </div>
          )}

          {/* In-person: venue */}
          {!isVirtual && (
            <div className="bg-amber-50 rounded-lg p-4 space-y-3">
              <p className="text-xs font-semibold text-amber-800 uppercase tracking-wide">Venue Details</p>
              <div>
                <label className="block text-xs font-medium text-amber-700 mb-1">Venue Address</label>
                <textarea value={form.venueAddress || ""} onChange={(e) => update("venueAddress", e.target.value)}
                  placeholder="Conference Room 3, 10 Downing Street, London SW1A 2AA"
                  rows={2}
                  className="w-full border border-amber-200 rounded-lg px-3 py-2 text-sm bg-white" />
              </div>
            </div>
          )}

          {/* Notes */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Notes</label>
            <textarea value={form.notes || ""} onChange={(e) => update("notes", e.target.value)}
              placeholder="Any additional details..."
              rows={2}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>

          {/* Attendees (optional) */}
          <div className="border-t border-gray-200 pt-5">
            <div className="flex items-center justify-between mb-3">
              <p className="text-xs font-semibold text-gray-700 uppercase tracking-wide">Attendees (optional)</p>
              <button type="button" onClick={addAttendeeRow}
                className="flex items-center gap-1.5 text-xs text-brand-600 hover:text-brand-700 font-medium">
                <UserPlus className="w-3.5 h-3.5" /> Add Attendee
              </button>
            </div>
            {attendees.length === 0 && (
              <p className="text-xs text-gray-400">No attendees yet — you can add them now or later from the course detail page.</p>
            )}
            {attendees.map((row, i) => (
              <div key={i} className="flex gap-2 items-start mb-2">
                <div className="grid grid-cols-3 gap-2 flex-1">
                  <input type="text" placeholder="First name" value={row.firstName}
                    onChange={(e) => updateAttendee(i, "firstName", e.target.value)}
                    className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm" />
                  <input type="text" placeholder="Last name" value={row.lastName}
                    onChange={(e) => updateAttendee(i, "lastName", e.target.value)}
                    className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm" />
                  <input type="email" placeholder="Email" value={row.email}
                    onChange={(e) => updateAttendee(i, "email", e.target.value)}
                    className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm" />
                </div>
                <button type="button" onClick={() => removeAttendee(i)} className="text-gray-400 hover:text-red-500 mt-1.5">
                  <Trash2 className="w-4 h-4" />
                </button>
              </div>
            ))}
          </div>

          {/* Submit */}
          <div className="flex gap-3 pt-4 border-t border-gray-200">
            <Button type="submit" disabled={saving}>
              {saving ? "Creating..." : `Create Course${attendees.filter((a) => a.email).length > 0 ? ` + ${attendees.filter((a) => a.email).length} Attendees` : ""}`}
            </Button>
            <Button variant="secondary" type="button" onClick={onClose}>Cancel</Button>
          </div>
        </form>
      )}
    </SlideOver>
  );
}
