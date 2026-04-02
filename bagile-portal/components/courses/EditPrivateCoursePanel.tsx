"use client";

import { useState, useEffect } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { OrganisationTypeAhead } from "@/components/ui/OrganisationTypeAhead";
import { CourseScheduleDetail, UpdatePrivateCourseRequest, updatePrivateCourse, Trainer, getTrainers, OrgSummary } from "@/lib/api";
import { generateCourseName, generateInvoiceRef } from "@/lib/privateCourseHelpers";
import { RotateCcw } from "lucide-react";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  course: CourseScheduleDetail;
  onSaved: () => void;
}

function toDateInput(iso: string | null | undefined): string {
  if (!iso) return "";
  return iso.slice(0, 10);
}

// ── Section header ──────────────────────────────────────────────────────────
function SectionHeader({ label }: { label: string }) {
  return (
    <div className="pt-2 pb-1">
      <p className="text-xs font-semibold text-gray-400 uppercase tracking-wider">{label}</p>
      <div className="border-t border-gray-100 mt-1" />
    </div>
  );
}

export function EditPrivateCoursePanel({ open, onClose, apiKey, course, onSaved }: Props) {
  // Organisation
  const [org, setOrg] = useState<OrgSummary | null>(null);

  // Editable fields
  const [courseName, setCourseName] = useState("");
  const [nameOverridden, setNameOverridden] = useState(true); // Default true in edit: preserve what's there
  const [trainerName, setTrainerName] = useState<string | undefined>();
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [capacity, setCapacity] = useState<number | undefined>();
  const [price, setPrice] = useState<number | undefined>();
  // courseRef = the course reference code (PSM-FNC-270426), stored as invoiceReference on backend
  const [courseRef, setCourseRef] = useState("");
  const [refOverridden, setRefOverridden] = useState(true); // Default true in edit: preserve existing ref
  const [venueAddress, setVenueAddress] = useState("");
  const [meetingUrl, setMeetingUrl] = useState("");
  const [meetingId, setMeetingId] = useState("");
  const [meetingPasscode, setMeetingPasscode] = useState("");
  const [notes, setNotes] = useState("");

  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [trainers, setTrainers] = useState<Trainer[]>([]);

  // Load trainers once on mount
  useEffect(() => {
    if (!apiKey) return;
    getTrainers(apiKey).then(setTrainers).catch(() => {});
  }, [apiKey]);

  const isVirtual = (course.formatType ?? "").toLowerCase().includes("virtual");

  // Seed form from course when panel opens
  useEffect(() => {
    if (!open) return;
    setError("");
    setNameOverridden(true);
    setRefOverridden(true);

    setCourseName(course.title ?? "");
    setTrainerName(course.trainerName ?? undefined);
    setStartDate(toDateInput(course.startDate));
    setEndDate(toDateInput(course.endDate ?? course.startDate));
    setCapacity(course.capacity ?? undefined);
    setPrice(course.price ?? undefined);
    setCourseRef(course.invoiceReference ?? "");
    setVenueAddress(course.venueAddress ?? "");
    setMeetingUrl(course.meetingUrl ?? "");
    setMeetingId(course.meetingId ?? "");
    setMeetingPasscode(course.meetingPasscode ?? "");
    setNotes(course.notes ?? "");

    // Pre-populate org from existing data
    if (course.clientOrganisationId && course.clientOrganisationName) {
      setOrg({
        id: course.clientOrganisationId,
        name: course.clientOrganisationName,
        acronym: course.clientOrganisationAcronym ?? null,
        partnerType: null,
        ptnTier: null,
      });
    } else {
      setOrg(null);
    }
  }, [open, course]);

  // Auto-generate course name when not overridden
  useEffect(() => {
    if (nameOverridden) return;
    setCourseName(generateCourseName(course.courseCode ?? "", org?.name ?? "", course.formatType ?? "virtual"));
  }, [org, nameOverridden, course.courseCode, course.formatType]);

  // Auto-generate reference when not overridden
  useEffect(() => {
    if (refOverridden) return;
    setCourseRef(generateInvoiceRef(course.courseCode ?? "", org?.acronym ?? "", startDate));
  }, [org, startDate, refOverridden, course.courseCode]);

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!courseName || !startDate || !endDate) {
      setError("Course name, start date, and end date are required");
      return;
    }
    setSaving(true);
    setError("");
    try {
      const payload: UpdatePrivateCourseRequest = {
        name: courseName,
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
      };
      await updatePrivateCourse(apiKey, course.id, payload);
      onSaved();
      onClose();
    } catch {
      setError("Failed to save changes — please try again");
    } finally {
      setSaving(false);
    }
  }

  return (
    <SlideOver open={open} onClose={onClose} title="Edit Course" subtitle={course.courseCode ?? undefined}>
      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

      <form onSubmit={handleSubmit} className="space-y-4">

        {/* ── COURSE ── */}
        <SectionHeader label="Course" />

        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Organisation</label>
          <OrganisationTypeAhead
            apiKey={apiKey}
            value={org}
            onSelect={(selected) => {
              setOrg(selected);
            }}
            placeholder="Search or create organisation…"
          />
          {org && !nameOverridden && (
            <p className="mt-1 text-xs text-gray-400">Course name will update to reflect this org.</p>
          )}
        </div>

        {/* ── SCHEDULE ── */}
        <SectionHeader label="Schedule" />

        <div>
          <label className="block text-xs font-medium text-gray-700 mb-1">Trainer</label>
          {trainers.length > 0 ? (
            <select value={trainerName ?? ""} onChange={(e) => setTrainerName(e.target.value || undefined)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm bg-white">
              <option value="">— Select trainer —</option>
              {trainers.map((t) => <option key={t.id} value={t.name}>{t.name}</option>)}
            </select>
          ) : (
            <input type="text" value={trainerName ?? ""} onChange={(e) => setTrainerName(e.target.value || undefined)}
              placeholder="Alex Brown"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          )}
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

        {/* ── COMMERCIAL ── */}
        <SectionHeader label="Commercial" />

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Price (total £)</label>
            <input type="number" value={price ?? ""} onChange={(e) => setPrice(e.target.value ? Number(e.target.value) : undefined)}
              placeholder="5000" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Capacity</label>
            <input type="number" value={capacity ?? ""} onChange={(e) => setCapacity(e.target.value ? Number(e.target.value) : undefined)}
              placeholder="20" className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm" />
          </div>
        </div>

        {/* ── AUTO-GENERATED ── */}
        <SectionHeader label="Auto-generated" />

        {/* Reference — editable with option to regenerate */}
        <div>
          <div className="flex items-center justify-between mb-1">
            <label className="block text-xs font-medium text-gray-700">Reference</label>
            {refOverridden && org && startDate && (
              <button type="button" onClick={() => setRefOverridden(false)}
                className="flex items-center gap-1 text-xs text-gray-400 hover:text-brand-600">
                <RotateCcw className="w-3 h-3" /> Regenerate from org + date
              </button>
            )}
          </div>
          <input
            type="text"
            value={courseRef}
            onChange={(e) => { setCourseRef(e.target.value); setRefOverridden(true); }}
            onBlur={(e) => { if (!e.target.value.trim()) setRefOverridden(false); }}
            placeholder="e.g. PSM-FNC-270426"
            className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono"
          />
        </div>

        {/* Course name — pre-populated, editable, can reset to auto */}
        <div>
          <div className="flex items-center justify-between mb-1">
            <label className="block text-xs font-medium text-gray-700 text-gray-400">Course Name</label>
            {nameOverridden && (
              <button type="button" onClick={() => setNameOverridden(false)}
                className="flex items-center gap-1 text-xs text-gray-400 hover:text-brand-600">
                <RotateCcw className="w-3 h-3" /> Regenerate from org
              </button>
            )}
          </div>
          <input
            type="text"
            value={courseName}
            onChange={(e) => { setCourseName(e.target.value); setNameOverridden(true); }}
            required
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

        {/* Actions */}
        <div className="flex gap-3 pt-4 border-t border-gray-200">
          <Button type="submit" disabled={saving}>
            {saving ? "Saving..." : "Save Changes"}
          </Button>
          <Button variant="secondary" type="button" onClick={onClose}>Cancel</Button>
        </div>
      </form>
    </SlideOver>
  );
}
