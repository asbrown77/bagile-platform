"use client";

import { useState, useEffect } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { OrganisationTypeAhead } from "@/components/ui/OrganisationTypeAhead";
import {
  createPrivateCourse,
  addPrivateAttendees,
  getScheduleConflicts,
  CreatePrivateCourseRequest,
  AttendeeInput,
  ScheduleConflict,
  Trainer,
  getTrainers,
  OrgSummary,
} from "@/lib/api";
import { generateCourseName, generateInvoiceRef } from "@/lib/privateCourseHelpers";
import { Trash2, UserPlus, Copy, FileJson, RotateCcw } from "lucide-react";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  onCreated: () => void;
}

const COURSE_CODES = ["PSM", "PSPO", "PSPOAI", "PSPOA", "PALE", "PSK", "PSMA", "PSMAI", "PSFS", "APS", "EBM", "PSU"];

const JSON_TEMPLATE = `{
  "organisationName": "Frazer-Nash Consultancy Ltd",
  "organisationAcronym": "FNC",
  "courseCode": "PSM",
  "startDate": "2026-04-15",
  "endDate": "2026-04-16",
  "formatType": "virtual",
  "trainerName": "Alex Brown",
  "capacity": 20,
  "price": 5000,
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

// ── Section header ──────────────────────────────────────────────────────────
function SectionHeader({ label }: { label: string }) {
  return (
    <div className="pt-2 pb-1">
      <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">{label}</p>
      <div className="border-t border-gray-100 mt-1" />
    </div>
  );
}

export function CreatePrivateCoursePanel({ open, onClose, apiKey, onCreated }: Props) {
  const [mode, setMode] = useState("form");

  // Selected organisation
  const [org, setOrg] = useState<OrgSummary | null>(null);

  // Core form fields
  const [courseCode, setCourseCode] = useState("PSM");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [formatType, setFormatType] = useState("virtual");
  const [trainerName, setTrainerName] = useState<string | undefined>();
  const [capacity, setCapacity] = useState<number | undefined>();
  const [price, setPrice] = useState<number | undefined>();
  const [meetingUrl, setMeetingUrl] = useState("");
  const [meetingId, setMeetingId] = useState("");
  const [meetingPasscode, setMeetingPasscode] = useState("");
  const [venueAddress, setVenueAddress] = useState("");
  const [notes, setNotes] = useState("");

  // Auto-generated fields with override flags
  // courseRef = the course reference code (PSM-FNC-270426), stored as invoiceReference on backend
  const [courseRef, setCourseRef] = useState("");
  const [refOverridden, setRefOverridden] = useState(false);
  const [courseName, setCourseName] = useState("");
  const [nameOverridden, setNameOverridden] = useState(false);

  const [attendees, setAttendees] = useState<AttendeeInput[]>([]);
  const [jsonText, setJsonText] = useState("");
  const [jsonParsed, setJsonParsed] = useState<{ course: CreatePrivateCourseRequest; attendees: AttendeeInput[]; org?: OrgSummary } | null>(null);
  const [conflicts, setConflicts] = useState<ScheduleConflict[]>([]);
  const [trainers, setTrainers] = useState<Trainer[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [copied, setCopied] = useState(false);

  // Load trainers once apiKey is available
  useEffect(() => {
    if (!apiKey) return;
    getTrainers(apiKey)
      .then((list) => {
        setTrainers(list);
        if (list.length > 0 && !trainerName) setTrainerName(list[0].name);
      })
      .catch(() => {});
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiKey]);

  // Check for conflicts when dates or trainer change
  useEffect(() => {
    if (!apiKey || !startDate || !endDate) { setConflicts([]); return; }
    const timer = setTimeout(() => {
      getScheduleConflicts(apiKey, startDate, endDate, trainerName)
        .then(setConflicts)
        .catch(() => setConflicts([]));
    }, 500);
    return () => clearTimeout(timer);
  }, [apiKey, startDate, endDate, trainerName]);

  // Auto-generate reference code
  useEffect(() => {
    if (refOverridden) return;
    setCourseRef(generateInvoiceRef(courseCode, org?.acronym ?? "", startDate));
  }, [courseCode, org, startDate, refOverridden]);

  // Auto-generate course name
  useEffect(() => {
    if (nameOverridden) return;
    setCourseName(generateCourseName(courseCode, org?.name ?? "", formatType));
  }, [courseCode, org, formatType, nameOverridden]);

  function resetForm() {
    setOrg(null);
    setCourseCode("PSM");
    setStartDate("");
    setEndDate("");
    setFormatType("virtual");
    setTrainerName(trainers.length > 0 ? trainers[0].name : undefined);
    setCapacity(undefined);
    setPrice(undefined);
    setMeetingUrl("");
    setMeetingId("");
    setMeetingPasscode("");
    setVenueAddress("");
    setNotes("");
    setCourseRef("");
    setRefOverridden(false);
    setCourseName("");
    setNameOverridden(false);
    setAttendees([]);
    setJsonText("");
    setJsonParsed(null);
    setError("");
  }

  function addAttendeeRow() { setAttendees((a) => [...a, emptyAttendee()]); }
  function updateAttendee(i: number, f: keyof AttendeeInput, v: string) {
    setAttendees((rows) => rows.map((r, idx) => idx === i ? { ...r, [f]: v } : r));
  }
  function removeAttendee(i: number) { setAttendees((rows) => rows.filter((_, idx) => idx !== i)); }

  function parseJson() {
    try {
      const data = JSON.parse(jsonText);
      const orgFromJson: OrgSummary | undefined = data.organisationName
        ? { id: 0, name: data.organisationName, acronym: data.organisationAcronym ?? null, partnerType: null, ptnTier: null }
        : undefined;
      const course: CreatePrivateCourseRequest = {
        name: data.name || generateCourseName(data.courseCode || "PSM", data.organisationName || "", data.formatType || "virtual"),
        courseCode: data.courseCode || "PSM",
        startDate: data.startDate || "",
        endDate: data.endDate || "",
        formatType: data.formatType || "virtual",
        trainerName: data.trainerName,
        capacity: data.capacity,
        price: data.price,
        invoiceReference: data.invoiceReference || generateInvoiceRef(data.courseCode || "PSM", data.organisationAcronym || "", data.startDate || ""),
        meetingUrl: data.meetingUrl,
        meetingId: data.meetingId,
        meetingPasscode: data.meetingPasscode,
        venueAddress: data.venueAddress,
        notes: data.notes,
      };
      const atts: AttendeeInput[] = (data.attendees || []).map((a: AttendeeInput & { [key: string]: string }) => ({
        firstName: a.firstName || "", lastName: a.lastName || "", email: a.email || "",
        company: a.company, country: a.country,
      }));
      setJsonParsed({ course, attendees: atts, org: orgFromJson });
      setError("");
    } catch {
      setError("Invalid JSON — check the format and try again");
      setJsonParsed(null);
    }
  }

  async function handleSubmit(courseData: CreatePrivateCourseRequest, attendeeList: AttendeeInput[], submitOrg?: OrgSummary | null) {
    if (!apiKey) { setError("API key not loaded — please refresh and try again"); return; }
    if (!courseData.name || !courseData.startDate || !courseData.endDate) {
      setError("Course name, start date, and end date are required"); return;
    }
    setError("");
    setSaving(true);
    try {
      const payload: CreatePrivateCourseRequest = {
        ...courseData,
        clientOrganisationId: submitOrg?.id && submitOrg.id > 0 ? submitOrg.id : undefined,
      };
      const created = await createPrivateCourse(apiKey, payload);

      if (attendeeList.length > 0) {
        const valid = attendeeList.filter((a) => a.email.trim());
        if (valid.length > 0) await addPrivateAttendees(apiKey, created.id, valid);
      }

      resetForm();
      onCreated();
      onClose();
      window.location.href = `/courses/${created.id}`;
    } catch {
      setError("Failed to create course");
    } finally {
      setSaving(false);
    }
  }

  function formToRequest(): CreatePrivateCourseRequest {
    return {
      name: courseName,
      courseCode,
      startDate,
      endDate,
      formatType,
      trainerName,
      capacity,
      price,
      invoiceReference: courseRef || undefined,
      meetingUrl: meetingUrl || undefined,
      meetingId: meetingId || undefined,
      meetingPasscode: meetingPasscode || undefined,
      venueAddress: venueAddress || undefined,
      notes: notes || undefined,
    };
  }

  function copyTemplate() {
    navigator.clipboard.writeText(JSON_TEMPLATE);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  }

  const isVirtual = formatType === "virtual";

  return (
    <SlideOver open={open} onClose={onClose} title="Create Private Course" subtitle="Add a new private/corporate course" wide>
      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

      {/* ── JSON Import Mode ───────────────────────────────── */}
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
              Copy this template, fill in the client details, then paste below.
            </p>
            <pre className="bg-gray-900 text-gray-300 rounded-lg p-3 text-xs overflow-x-auto max-h-40 overflow-y-auto">
              {JSON_TEMPLATE}
            </pre>
          </div>
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
              <FileJson className="w-3.5 h-3.5" /> Parse and Preview
            </Button>
          </div>
          {jsonParsed && (
            <div className="bg-green-50 rounded-lg p-4 space-y-3">
              <p className="text-xs font-semibold text-green-800 uppercase tracking-wide">Preview</p>
              <div className="text-sm text-green-900 space-y-1">
                <p><strong>{jsonParsed.course.name}</strong></p>
                <p>{jsonParsed.course.courseCode} — {jsonParsed.course.formatType} — {jsonParsed.course.startDate} to {jsonParsed.course.endDate}</p>
                {jsonParsed.org && <p>Organisation: {jsonParsed.org.name}{jsonParsed.org.acronym ? ` (${jsonParsed.org.acronym})` : ""}</p>}
                {jsonParsed.course.trainerName && <p>Trainer: {jsonParsed.course.trainerName}</p>}
                {jsonParsed.course.invoiceReference && <p>Reference: {jsonParsed.course.invoiceReference}</p>}
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
                <Button onClick={() => handleSubmit(jsonParsed.course, jsonParsed.attendees, jsonParsed.org)} disabled={saving || !apiKey}>
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

      {/* ── Form Mode ──────────────────────────────────────── */}
      {mode === "form" && (
        <form onSubmit={(e) => { e.preventDefault(); handleSubmit(formToRequest(), attendees, org); }} className="space-y-4">

          {/* ── COURSE ── */}
          <SectionHeader label="Course" />

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Course Type</label>
            <select value={courseCode} onChange={(e) => setCourseCode(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white">
              {COURSE_CODES.map((c) => <option key={c} value={c}>{c}</option>)}
            </select>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Organisation</label>
            <OrganisationTypeAhead
              apiKey={apiKey}
              value={org}
              onSelect={setOrg}
              placeholder="Start typing to search or create…"
            />
          </div>

          {/* ── SCHEDULE ── */}
          <SectionHeader label="Schedule" />

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Format</label>
            <select value={formatType} onChange={(e) => setFormatType(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white">
              <option value="virtual">Virtual</option>
              <option value="in_person">In-person</option>
            </select>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Start Date</label>
              <input type="date" value={startDate} onChange={(e) => setStartDate(e.target.value)} required
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">End Date</label>
              <input type="date" value={endDate} onChange={(e) => setEndDate(e.target.value)} required
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Trainer</label>
            {trainers.length > 0 ? (
              <select value={trainerName ?? ""} onChange={(e) => setTrainerName(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white">
                <option value="">— Select trainer —</option>
                {trainers.map((t) => <option key={t.id} value={t.name}>{t.name}</option>)}
              </select>
            ) : (
              <input type="text" value={trainerName || ""} onChange={(e) => setTrainerName(e.target.value)}
                placeholder="Alex Brown"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            )}
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

          {/* ── COMMERCIAL ── */}
          <SectionHeader label="Commercial" />

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Price (total)</label>
              <input type="number" value={price || ""} onChange={(e) => setPrice(e.target.value ? Number(e.target.value) : undefined)}
                placeholder="5000"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Capacity</label>
              <input type="number" value={capacity || ""} onChange={(e) => setCapacity(e.target.value ? Number(e.target.value) : undefined)}
                placeholder="20"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
            </div>
          </div>

          {/* ── AUTO-GENERATED ── */}
          <SectionHeader label="Auto-generated" />

          {/* Reference (course code) — primary identifier */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs font-medium text-gray-700">Reference</label>
              {refOverridden && (
                <button type="button" onClick={() => setRefOverridden(false)}
                  className="flex items-center gap-1 text-xs text-gray-400 hover:text-brand-600">
                  <RotateCcw className="w-3 h-3" /> Reset to auto
                </button>
              )}
            </div>
            <input
              type="text"
              value={courseRef}
              onChange={(e) => {
                setCourseRef(e.target.value);
                setRefOverridden(true);
              }}
              onBlur={(e) => { if (!e.target.value.trim()) setRefOverridden(false); }}
              placeholder="Auto-generated e.g. PSM-FNC-270426"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono"
            />
            <p className="mt-1 text-xs text-gray-400">Generated from course type, org acronym, and start date. This is the primary identifier used everywhere.</p>
          </div>

          {/* Course name — secondary, display label */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-xs font-medium text-gray-700 text-gray-400">Course Name</label>
              {nameOverridden && (
                <button type="button" onClick={() => setNameOverridden(false)}
                  className="flex items-center gap-1 text-xs text-gray-400 hover:text-brand-600">
                  <RotateCcw className="w-3 h-3" /> Reset to auto
                </button>
              )}
            </div>
            <input
              type="text"
              value={courseName}
              onChange={(e) => { setCourseName(e.target.value); setNameOverridden(e.target.value !== ""); }}
              onBlur={(e) => { if (!e.target.value.trim()) setNameOverridden(false); }}
              required
              placeholder="Auto-generated from course type, org, and format"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm text-gray-600 focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
            />
          </div>

          {/* ── VENUE / MEETING (conditional) ── */}
          {!isVirtual && (
            <>
              <SectionHeader label="Venue Details" />
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">Venue Address</label>
                <textarea value={venueAddress} onChange={(e) => setVenueAddress(e.target.value)}
                  placeholder="Conference Room 3, 10 Downing Street, London SW1A 2AA"
                  rows={2}
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
              </div>
            </>
          )}

          {isVirtual && (
            <>
              <SectionHeader label="Meeting Details" />
              <div>
                <label className="block text-xs font-medium text-gray-700 mb-1">Zoom/Teams URL</label>
                <input type="url" value={meetingUrl} onChange={(e) => setMeetingUrl(e.target.value)}
                  placeholder="https://zoom.us/j/..."
                  className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">Meeting ID</label>
                  <input type="text" value={meetingId} onChange={(e) => setMeetingId(e.target.value)}
                    placeholder="123 456 7890"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
                </div>
                <div>
                  <label className="block text-xs font-medium text-gray-700 mb-1">Passcode</label>
                  <input type="text" value={meetingPasscode} onChange={(e) => setMeetingPasscode(e.target.value)}
                    placeholder="abc123"
                    className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
                </div>
              </div>
            </>
          )}

          {/* ── ADDITIONAL ── */}
          <SectionHeader label="Additional" />

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Notes</label>
            <textarea value={notes} onChange={(e) => setNotes(e.target.value)}
              placeholder="Any additional details..."
              rows={2}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>

          {/* ── ATTENDEES (optional) ── */}
          <div className="border-t border-gray-200 pt-4">
            <div className="flex items-center justify-between mb-3">
              <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Attendees (optional)</p>
              <button type="button" onClick={addAttendeeRow}
                className="flex items-center gap-1.5 text-xs text-brand-600 hover:text-brand-700 font-medium">
                <UserPlus className="w-3.5 h-3.5" /> Add Attendee
              </button>
            </div>
            {attendees.length === 0 && (
              <p className="text-xs text-gray-400">No attendees yet — add them now or later from the course page.</p>
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
            <Button type="submit" disabled={saving || !apiKey}>
              {saving ? "Creating..." : `Create Course${attendees.filter((a) => a.email).length > 0 ? ` + ${attendees.filter((a) => a.email).length} Attendees` : ""}`}
            </Button>
            <Button variant="secondary" type="button" onClick={onClose}>Cancel</Button>
          </div>
        </form>
      )}
    </SlideOver>
  );
}
