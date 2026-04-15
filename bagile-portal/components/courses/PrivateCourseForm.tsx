"use client";

import { useState, useEffect } from "react";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { OrganisationTypeAhead } from "@/components/ui/OrganisationTypeAhead";
import {
  CourseScheduleDetail,
  CreatePrivateCourseRequest,
  UpdatePrivateCourseRequest,
  createPrivateCourse,
  updatePrivateCourse,
  addPrivateAttendees,
  getScheduleConflicts,
  getTrainers,
  AttendeeInput,
  ScheduleConflict,
  Trainer,
  OrgSummary,
} from "@/lib/api";
import { generateCourseName, generateInvoiceRef } from "@/lib/privateCourseHelpers";
import { extractCourseTypeFromSku } from "@/lib/calendarHelpers";
import { RotateCcw, Trash2, UserPlus } from "lucide-react";

// ── Types ────────────────────────────────────────────────────────────────────

export interface PrivateCourseFormProps {
  mode: "create" | "edit";
  /** Existing course data — required in edit mode. */
  course?: CourseScheduleDetail;
  apiKey: string;
  onSuccess: () => void;
  onCancel: () => void;
}

// ── Constants ────────────────────────────────────────────────────────────────

const COURSE_CODES = [
  "PSM", "PSPO", "PSK", "PALE", "PALEBM", "PSFS", "APS", "APSSD", "PSU",
  "PSMAI", "PSPOAI", "PSPOA", "PSMA",
  "PSMPO",
  "ICP", "ICPATF", "ICPACC",
];

/** Default duration in days for each course type (determines auto end date on create). */
const COURSE_DURATIONS: Record<string, number> = {
  PSM: 2, PSPO: 2, PSK: 2, PALE: 2, PALEBM: 1, PSFS: 1, APS: 2, APSSD: 3, PSU: 2,
  PSMAI: 2, PSPOAI: 2, PSPOA: 2, PSMA: 2,
  PSMPO: 3,
  ICP: 2, ICPATF: 2, ICPACC: 3,
};

function addDays(dateStr: string, days: number): string {
  const d = new Date(dateStr);
  d.setDate(d.getDate() + days);
  return d.toISOString().slice(0, 10);
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function toDateInput(iso: string | null | undefined): string {
  if (!iso) return "";
  return iso.slice(0, 10);
}

// ── Sub-components ───────────────────────────────────────────────────────────

function SectionHeader({ label }: { label: string }) {
  return (
    <div className="pt-2 pb-1">
      <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">{label}</p>
      <div className="border-t border-gray-100 mt-1" />
    </div>
  );
}

// ── Main form ────────────────────────────────────────────────────────────────

export function PrivateCourseForm({ mode, course, apiKey, onSuccess, onCancel }: PrivateCourseFormProps) {
  const isEdit = mode === "edit";

  // ── Organisation ──────────────────────────────────────────────────────────

  const [org, setOrg] = useState<OrgSummary | null>(null);

  // ── Course identity ───────────────────────────────────────────────────────

  // In edit mode, extract the clean type code from the full SKU (e.g. "PSM-PRIV-270426" → "PSM")
  const [courseCode, setCourseCode] = useState(
    course?.courseCode ? extractCourseTypeFromSku(course.courseCode) : "PSM"
  );

  // In edit mode the format is immutable (not in UpdatePrivateCourseRequest).
  // We keep it in state for the venue/meeting toggle, but don't send it on save.
  const [formatType, setFormatType] = useState(
    course?.formatType ?? "virtual"
  );

  // ── Schedule ──────────────────────────────────────────────────────────────

  const [startDate, setStartDate] = useState(toDateInput(course?.startDate));
  const [endDate, setEndDate] = useState(toDateInput(course?.endDate ?? course?.startDate));
  const [trainerName, setTrainerName] = useState<string | undefined>(course?.trainerName ?? undefined);
  const [trainers, setTrainers] = useState<Trainer[]>([]);

  // ── Commercial ────────────────────────────────────────────────────────────

  const [price, setPrice] = useState<number | undefined>(course?.price ?? undefined);
  const [capacity, setCapacity] = useState<number | undefined>(course?.capacity ?? undefined);

  // ── Auto-generated reference + name ──────────────────────────────────────
  // In edit mode both start overridden (preserve what the API has).
  // In create mode both start as auto.

  const [courseRef, setCourseRef] = useState(course?.invoiceReference ?? "");
  const [refOverridden, setRefOverridden] = useState(isEdit);

  const [courseName, setCourseName] = useState(course?.title ?? "");
  const [nameOverridden, setNameOverridden] = useState(isEdit);

  // ── Venue / Meeting ───────────────────────────────────────────────────────

  const [venueAddress, setVenueAddress] = useState(course?.venueAddress ?? "");
  const [meetingUrl, setMeetingUrl] = useState(course?.meetingUrl ?? "");
  const [meetingId, setMeetingId] = useState(course?.meetingId ?? "");
  const [meetingPasscode, setMeetingPasscode] = useState(course?.meetingPasscode ?? "");

  // ── Status (edit mode only) ───────────────────────────────────────────────
  const [status, setStatus] = useState(course?.status ?? "confirmed");

  // ── Additional ────────────────────────────────────────────────────────────

  const [notes, setNotes] = useState(course?.notes ?? "");

  // ── Create-only: attendees ────────────────────────────────────────────────

  const [attendees, setAttendees] = useState<AttendeeInput[]>([]);

  // ── UX state ─────────────────────────────────────────────────────────────

  const [conflicts, setConflicts] = useState<ScheduleConflict[]>([]);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  const isVirtual = formatType.toLowerCase().includes("virtual");

  // ── Effects ───────────────────────────────────────────────────────────────

  // Seed org from existing course on mount (edit mode)
  useEffect(() => {
    if (isEdit && course?.clientOrganisationId && course.clientOrganisationName) {
      setOrg({
        id: course.clientOrganisationId,
        name: course.clientOrganisationName,
        acronym: course.clientOrganisationAcronym ?? null,
        partnerType: null,
        ptnTier: null,
      });
    }
  // Only run once on mount — course prop is stable within a panel lifecycle
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Load trainers; default to first trainer in create mode
  useEffect(() => {
    if (!apiKey) return;
    getTrainers(apiKey)
      .then((list) => {
        setTrainers(list);
        if (!isEdit && list.length > 0 && !trainerName) {
          setTrainerName(list[0].name);
        }
      })
      .catch(() => {});
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [apiKey]);

  // Conflict check — create mode only (edit: course is already scheduled)
  useEffect(() => {
    if (isEdit || !apiKey || !startDate || !endDate) {
      setConflicts([]);
      return;
    }
    const timer = setTimeout(() => {
      getScheduleConflicts(apiKey, startDate, endDate, trainerName)
        .then(setConflicts)
        .catch(() => setConflicts([]));
    }, 500);
    return () => clearTimeout(timer);
  }, [isEdit, apiKey, startDate, endDate, trainerName]);

  // Auto-set end date from course type duration (create mode only)
  useEffect(() => {
    if (isEdit || !startDate) return;
    const duration = COURSE_DURATIONS[courseCode.toUpperCase()] ?? 2;
    setEndDate(addDays(startDate, duration - 1));
  }, [isEdit, courseCode, startDate]);

  // Auto-generate reference
  useEffect(() => {
    if (refOverridden) return;
    setCourseRef(generateInvoiceRef(courseCode, org?.acronym ?? "", startDate));
  }, [courseCode, org, startDate, refOverridden]);

  // Auto-generate course name
  useEffect(() => {
    if (nameOverridden) return;
    setCourseName(generateCourseName(courseCode, org?.name ?? "", formatType));
  }, [courseCode, org, formatType, nameOverridden]);

  // ── Attendee helpers (create-only) ────────────────────────────────────────

  function addAttendeeRow() {
    setAttendees((rows) => [...rows, { firstName: "", lastName: "", email: "" }]);
  }

  function updateAttendee(index: number, field: keyof AttendeeInput, value: string) {
    setAttendees((rows) =>
      rows.map((row, i) => (i === index ? { ...row, [field]: value } : row))
    );
  }

  function removeAttendee(index: number) {
    setAttendees((rows) => rows.filter((_, i) => i !== index));
  }

  // ── Submit ────────────────────────────────────────────────────────────────

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    if (!courseName || !startDate || !endDate) {
      setError("Course name, start date, and end date are required");
      return;
    }

    setSaving(true);
    setError("");

    try {
      if (isEdit && course) {
        const payload: UpdatePrivateCourseRequest = {
          name: courseName,
          courseCode,
          trainerName: trainerName || undefined,
          startDate,
          endDate,
          capacity,
          price,
          clientOrganisationId: org?.id ?? undefined,
          invoiceReference: courseRef || undefined,
          venueAddress: venueAddress || undefined,
          meetingUrl: meetingUrl || undefined,
          meetingId: meetingId || undefined,
          meetingPasscode: meetingPasscode || undefined,
          notes: notes || undefined,
          status,
        };
        await updatePrivateCourse(apiKey, course.id, payload);
        onSuccess();
        onCancel();
      } else {
        const payload: CreatePrivateCourseRequest = {
          name: courseName,
          courseCode,
          startDate,
          endDate,
          formatType,
          trainerName: trainerName || undefined,
          capacity,
          price,
          clientOrganisationId: org?.id && org.id > 0 ? org.id : undefined,
          invoiceReference: courseRef || undefined,
          venueAddress: venueAddress || undefined,
          meetingUrl: meetingUrl || undefined,
          meetingId: meetingId || undefined,
          meetingPasscode: meetingPasscode || undefined,
          notes: notes || undefined,
        };
        const created = await createPrivateCourse(apiKey, payload);

        const validAttendees = attendees.filter((a) => a.email.trim());
        if (validAttendees.length > 0) {
          await addPrivateAttendees(apiKey, created.id, validAttendees);
        }

        onSuccess();
        onCancel();
        window.location.href = `/courses/${created.id}`;
      }
    } catch {
      setError(isEdit ? "Failed to save changes — please try again" : "Failed to create course");
    } finally {
      setSaving(false);
    }
  }

  // ── Render ────────────────────────────────────────────────────────────────

  const validAttendeeCount = attendees.filter((a) => a.email).length;

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && (
        <div className="mb-4">
          <AlertBanner variant="danger">{error}</AlertBanner>
        </div>
      )}

      {/* ── COURSE ── */}
      <SectionHeader label="Course" />

      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">Organisation</label>
        <OrganisationTypeAhead
          apiKey={apiKey}
          value={org}
          onSelect={setOrg}
          placeholder="Start typing to search or create…"
        />
        {org && !nameOverridden && (
          <p className="mt-1 text-xs text-gray-400">Course name will update to reflect this org.</p>
        )}
      </div>

      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">Course Type</label>
        <select
          value={courseCode}
          onChange={(e) => {
            setCourseCode(e.target.value);
            // Changing course type should regenerate name and reference
            setRefOverridden(false);
            setNameOverridden(false);
          }}
          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white"
        >
          {COURSE_CODES.map((c) => (
            <option key={c} value={c}>{c}</option>
          ))}
        </select>
      </div>

      {/* ── SCHEDULE ── */}
      <SectionHeader label="Schedule" />

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Start Date</label>
          <input
            type="date"
            value={startDate}
            onChange={(e) => setStartDate(e.target.value)}
            required
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">End Date</label>
          <input
            type="date"
            value={endDate}
            onChange={(e) => setEndDate(e.target.value)}
            required
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        </div>
      </div>

      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">Format</label>
        {isEdit ? (
          <p className="text-sm text-gray-700 py-2 px-3 bg-gray-50 border border-gray-200 rounded-lg">
            {isVirtual ? "Virtual" : "In-person"}
          </p>
        ) : (
          <select
            value={formatType}
            onChange={(e) => setFormatType(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white"
          >
            <option value="virtual">Virtual</option>
            <option value="in_person">In-person</option>
          </select>
        )}
      </div>

      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">Trainer</label>
        {trainers.length > 0 ? (
          <select
            value={trainerName ?? ""}
            onChange={(e) => setTrainerName(e.target.value || undefined)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white"
          >
            <option value="">— Select trainer —</option>
            {trainers.map((t) => (
              <option key={t.id} value={t.name}>{t.name}</option>
            ))}
          </select>
        ) : (
          <input
            type="text"
            value={trainerName ?? ""}
            onChange={(e) => setTrainerName(e.target.value || undefined)}
            placeholder="Alex Brown"
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        )}
      </div>

      {/* Conflict warning — create mode only */}
      {!isEdit && conflicts.length > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-lg p-3">
          <p className="text-xs font-semibold text-amber-800 mb-1.5">
            {conflicts.length} course{conflicts.length !== 1 ? "s" : ""} overlap these dates:
          </p>
          <ul className="space-y-1">
            {conflicts.slice(0, 5).map((c) => (
              <li key={c.conflictingCourseId} className="text-xs text-amber-700 flex items-center gap-2">
                <span
                  className={`w-2 h-2 rounded-full ${
                    c.conflictType === "trainer_clash" ? "bg-red-500" : "bg-amber-400"
                  }`}
                />
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
          <label className="block text-xs font-medium text-gray-700 mb-1">Price (total £)</label>
          <input
            type="number"
            value={price ?? ""}
            onChange={(e) => setPrice(e.target.value ? Number(e.target.value) : undefined)}
            placeholder="5000"
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        </div>
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Capacity</label>
          <input
            type="number"
            value={capacity ?? ""}
            onChange={(e) => setCapacity(e.target.value ? Number(e.target.value) : undefined)}
            placeholder="20"
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
          />
        </div>
      </div>

      {/* ── DETAILS ── */}
      <SectionHeader label="Details" />

      <div>
        <div className="flex items-center justify-between mb-1">
          <label className="block text-xs font-medium text-gray-700">Course Name</label>
          {nameOverridden && (
            <button
              type="button"
              onClick={() => setNameOverridden(false)}
              className="flex items-center gap-1 text-xs text-gray-400 hover:text-brand-600"
            >
              <RotateCcw className="w-3 h-3" /> Regenerate from org
            </button>
          )}
        </div>
        <input
          type="text"
          value={courseName}
          onChange={(e) => {
            setCourseName(e.target.value);
            setNameOverridden(e.target.value !== "");
          }}
          onBlur={(e) => { if (!e.target.value.trim()) setNameOverridden(false); }}
          required
          placeholder="Auto-generated from course type, org, and format"
          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm text-gray-600 focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
        />
      </div>

      {/* ── VENUE (in-person only) ── */}
      {!isVirtual && (
        <>
          <SectionHeader label="Venue Details" />
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Venue Address</label>
            <textarea
              value={venueAddress}
              onChange={(e) => setVenueAddress(e.target.value)}
              placeholder="Conference Room 3, 10 Downing Street, London SW1A 2AA"
              rows={2}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>
        </>
      )}

      {/* ── MEETING (virtual only) ── */}
      {isVirtual && (
        <>
          <SectionHeader label="Meeting Details" />
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Zoom/Teams URL</label>
            <input
              type="url"
              value={meetingUrl}
              onChange={(e) => setMeetingUrl(e.target.value)}
              placeholder="https://zoom.us/j/..."
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Meeting ID</label>
              <input
                type="text"
                value={meetingId}
                onChange={(e) => setMeetingId(e.target.value)}
                placeholder="123 456 7890"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Passcode</label>
              <input
                type="text"
                value={meetingPasscode}
                onChange={(e) => setMeetingPasscode(e.target.value)}
                placeholder="abc123"
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
              />
            </div>
          </div>
        </>
      )}

      {/* ── ADDITIONAL ── */}
      <SectionHeader label="Additional" />

      {isEdit && (
        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Status</label>
          <select
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white"
          >
            <option value="enquiry">Enquiry</option>
            <option value="quoted">Quoted</option>
            <option value="confirmed">Confirmed</option>
            <option value="completed">Completed</option>
            <option value="cancelled">Cancelled</option>
          </select>
        </div>
      )}

      <div>
        <label className="block text-xs font-medium text-gray-700 mb-1">Notes</label>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          placeholder="Any additional details..."
          rows={2}
          className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
        />
      </div>

      {/* ── ATTENDEES (create mode only) ── */}
      {!isEdit && (
        <div className="border-t border-gray-200 pt-4">
          <div className="flex items-center justify-between mb-3">
            <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">Attendees (optional)</p>
            <button
              type="button"
              onClick={addAttendeeRow}
              className="flex items-center gap-1.5 text-xs text-brand-600 hover:text-brand-700 font-medium"
            >
              <UserPlus className="w-3.5 h-3.5" /> Add Attendee
            </button>
          </div>
          {attendees.length === 0 && (
            <p className="text-xs text-gray-400">No attendees yet — add them now or later from the course page.</p>
          )}
          {attendees.map((row, i) => (
            <div key={i} className="flex gap-2 items-start mb-2">
              <div className="grid grid-cols-3 gap-2 flex-1">
                <input
                  type="text"
                  placeholder="First name"
                  value={row.firstName}
                  onChange={(e) => updateAttendee(i, "firstName", e.target.value)}
                  className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm"
                />
                <input
                  type="text"
                  placeholder="Last name"
                  value={row.lastName}
                  onChange={(e) => updateAttendee(i, "lastName", e.target.value)}
                  className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm"
                />
                <input
                  type="email"
                  placeholder="Email"
                  value={row.email}
                  onChange={(e) => updateAttendee(i, "email", e.target.value)}
                  className="border border-gray-300 rounded-lg px-2 py-1.5 text-sm"
                />
              </div>
              <button
                type="button"
                onClick={() => removeAttendee(i)}
                className="text-gray-400 hover:text-red-500 mt-1.5"
              >
                <Trash2 className="w-4 h-4" />
              </button>
            </div>
          ))}
        </div>
      )}

      {/* ── Actions ── */}
      <div className="flex gap-3 pt-4 border-t border-gray-200">
        <Button type="submit" disabled={saving || !apiKey}>
          {saving
            ? isEdit ? "Saving..." : "Creating..."
            : isEdit
              ? "Save Changes"
              : `Create Course${validAttendeeCount > 0 ? ` + ${validAttendeeCount} Attendees` : ""}`}
        </Button>
        <Button variant="secondary" type="button" onClick={onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
