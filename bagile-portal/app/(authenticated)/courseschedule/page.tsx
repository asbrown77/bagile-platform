"use client";

import { useEffect, useState, useCallback, useRef, Suspense } from "react";
import FullCalendar from "@fullcalendar/react";
import dayGridPlugin from "@fullcalendar/daygrid";
import timeGridPlugin from "@fullcalendar/timegrid";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  CalendarEvent,
  Trainer,
  getCalendarEvents,
  getTrainers,
  createPlannedCourse,
  createPrivateCourse,
  publishGateway,
  updatePlannedCourse,
  formatDate,
} from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { Button } from "@/components/ui/Button";
import { Badge } from "@/components/ui/Badge";
import { SlideOver } from "@/components/ui/SlideOver";
import { Plus, Calendar as CalendarIcon, Lock, AlertCircle, ExternalLink, Users, X, Loader2, LayoutList, Search, Upload, Download } from "lucide-react";
import {
  getBadgeSrc,
  getCourseCodeDisplay,
  getCourseDisplayName,
  getStatusColour,
  getStatusBadgeVariant,
  getStatusLabel,
  isDeadlineUrgent,
  getApplicableGateways,
  COURSE_TYPE_OPTIONS,
} from "@/lib/calendarHelpers";
import { getTrainerColour, trainerInitials } from "@/lib/courseColours";
import { addOneDayStr } from "@/lib/dateUtils";
import { useSearchParams } from "next/navigation";
import { generateCourseName } from "@/lib/privateCourseHelpers";
import { CsvImportModal } from "@/components/courses/CsvImportModal";
import { bulkCreatePlannedCourses } from "@/lib/api";

// ── Types for FullCalendar ──────────────────────────────────

interface FCEventExtended {
  calendarEvent: CalendarEvent;
}

// ── Toast notification ──────────────────────────────────────

function Toast({ message, type, onClose }: { message: string; type: "success" | "error"; onClose: () => void }) {
  useEffect(() => {
    const t = setTimeout(onClose, 4000);
    return () => clearTimeout(t);
  }, [onClose]);

  return (
    <div className={`fixed bottom-4 right-4 z-[100] px-4 py-3 rounded-lg shadow-lg text-sm font-medium
      ${type === "success" ? "bg-green-600 text-white" : "bg-red-600 text-white"}`}>
      {message}
    </div>
  );
}

// ── Course block (FullCalendar event content) ───────────────

function CourseBlock({ event }: { event: CalendarEvent; compact?: boolean }) {
  const badgeSrc = getBadgeSrc(event.courseType);
  const codeDisplay = getCourseCodeDisplay(event.courseType);
  const initials = event.trainerInitials || trainerInitials(event.trainerName);
  const avatarColour = getTrainerColour(initials);
  const statusColour = getStatusColour(event.status);
  const deadlineUrgent = isDeadlineUrgent(event.decisionDeadline);
  const isCancelled = event.status === "cancelled";

  // Enrolment display
  const enrolCount = event.status === "planned" ? 0 : event.enrolmentCount;

  return (
    <div
      className={`flex items-center gap-1.5 px-1.5 py-1 rounded text-xs cursor-pointer overflow-hidden relative
        ${isCancelled ? "opacity-50" : "hover:opacity-90"}`}
      style={{ borderLeft: `3px solid ${statusColour}` }}
    >
      {/* Badge image or abbreviation */}
      {badgeSrc ? (
        <img src={badgeSrc} alt={event.courseType} className="w-5 h-5 rounded-sm shrink-0 object-contain" />
      ) : (
        <div className="w-5 h-5 rounded-sm bg-gray-200 text-gray-600 text-[8px] font-bold flex items-center justify-center shrink-0">
          {event.courseType.slice(0, 3)}
        </div>
      )}

      {/* Course code + trainer */}
      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-1">
          <span className={`font-semibold text-gray-800 truncate ${isCancelled ? "line-through" : ""}`}>
            {codeDisplay}
          </span>
          <span className="text-gray-400 shrink-0">&middot;</span>
          <span
            className="inline-flex items-center justify-center rounded-full text-white font-bold shrink-0"
            style={{ backgroundColor: avatarColour, width: 14, height: 14, fontSize: 7 }}
          >
            {initials}
          </span>
        </div>
        <div className="text-[10px] text-gray-500 flex items-center gap-0.5">
          <Users className="w-2.5 h-2.5" />{enrolCount}
        </div>
      </div>

      {/* Private indicator */}
      {event.isPrivate && (
        <Lock className="w-3 h-3 text-amber-500 absolute top-0.5 right-0.5" />
      )}

      {/* Deadline urgent dot */}
      {deadlineUrgent && (
        <span className="absolute bottom-0.5 right-0.5 w-2 h-2 rounded-full bg-red-500" title="Decision deadline approaching" />
      )}
    </div>
  );
}

// ── Add Course Modal ────────────────────────────────────────

interface AddCourseModalProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: {
    courseType: string;
    trainerId: number;
    startDate: string;
    endDate: string;
    isVirtual: boolean;
    venue?: string;
    notes?: string;
    decisionDeadline?: string;
    isPrivate: boolean;
  }) => Promise<void>;
  trainers: Trainer[];
  initialValues?: {
    courseType: string;
    trainerId: number;
    startDate: string;
    endDate: string;
    isVirtual: boolean;
    venue: string;
    notes: string;
    decisionDeadline: string;
    isPrivate: boolean;
  };
  editMode?: boolean;
}

function AddCourseModal({ open, onClose, onSubmit, trainers, initialValues, editMode }: AddCourseModalProps) {
  const [courseType, setCourseType] = useState("PSM");
  const [trainerId, setTrainerId] = useState(0);
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [isVirtual, setIsVirtual] = useState(true);
  const [venue, setVenue] = useState("");
  const [notes, setNotes] = useState("");
  const [decisionDeadline, setDecisionDeadline] = useState("");
  const [isPrivate, setIsPrivate] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState("");

  // Default trainer to first available
  useEffect(() => {
    if (trainers.length > 0 && trainerId === 0) {
      setTrainerId(trainers[0].id);
    }
  }, [trainers, trainerId]);

  // Initialize from initialValues when opening in edit mode
  useEffect(() => {
    if (!open) return;
    if (initialValues) {
      setCourseType(initialValues.courseType);
      setTrainerId(initialValues.trainerId);
      setStartDate(initialValues.startDate);
      setEndDate(initialValues.endDate);
      setIsVirtual(initialValues.isVirtual);
      setVenue(initialValues.venue);
      setNotes(initialValues.notes);
      setDecisionDeadline(initialValues.decisionDeadline);
      setIsPrivate(initialValues.isPrivate);
    } else {
      reset();
    }
    setError("");
  }, [open]); // eslint-disable-line react-hooks/exhaustive-deps

  function reset() {
    setCourseType("PSM");
    setTrainerId(trainers[0]?.id || 0);
    setStartDate("");
    setEndDate("");
    setIsVirtual(true);
    setVenue("");
    setNotes("");
    setDecisionDeadline("");
    setIsPrivate(false);
    setError("");
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!startDate || !endDate) { setError("Start and end date are required"); return; }
    if (!trainerId) { setError("Please select a trainer"); return; }
    setSubmitting(true);
    setError("");
    try {
      await onSubmit({
        courseType,
        trainerId,
        startDate,
        endDate,
        isVirtual,
        venue: !isVirtual ? venue : undefined,
        notes: notes || undefined,
        decisionDeadline: decisionDeadline || undefined,
        isPrivate,
      });
      reset();
      onClose();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Failed to create course");
    } finally {
      setSubmitting(false);
    }
  }

  if (!open) return null;

  const modalTitle = editMode ? "Edit Planned Course" : isPrivate ? "Add Private Course" : "Add Planned Course";
  const submitLabel = editMode ? "Update Course" : isPrivate ? "Create Private Course" : "Create Course";

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      <div className="fixed inset-0 bg-black/40" onClick={onClose} />
      <div className="relative bg-white rounded-xl shadow-xl w-full max-w-lg mx-4 max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-200">
          <h2 className="text-lg font-semibold text-gray-900">{modalTitle}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 p-1 rounded-lg hover:bg-gray-100">
            <X className="w-5 h-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="px-6 py-4 space-y-4">
          {error && <AlertBanner variant="danger">{error}</AlertBanner>}

          {/* Course Type */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Course Type</label>
            <select
              value={courseType}
              onChange={(e) => setCourseType(e.target.value)}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
            >
              {COURSE_TYPE_OPTIONS.map((o) => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>

          {/* Trainer */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Trainer</label>
            <select
              value={trainerId}
              onChange={(e) => setTrainerId(Number(e.target.value))}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
            >
              {trainers.map((t) => (
                <option key={t.id} value={t.id}>{t.name}</option>
              ))}
            </select>
          </div>

          {/* Dates */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => {
                  setStartDate(e.target.value);
                  if (!endDate || endDate < e.target.value) setEndDate(e.target.value);
                }}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">End Date</label>
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                min={startDate}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              />
            </div>
          </div>

          {/* Virtual / Onsite */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Format</label>
            <div className="flex rounded-lg border border-gray-200 overflow-hidden w-fit">
              <button
                type="button"
                onClick={() => setIsVirtual(true)}
                className={`px-4 py-2 text-sm font-medium transition-colors
                  ${isVirtual ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
              >
                Virtual
              </button>
              <button
                type="button"
                onClick={() => setIsVirtual(false)}
                className={`px-4 py-2 text-sm font-medium border-l border-gray-200 transition-colors
                  ${!isVirtual ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
              >
                Onsite
              </button>
            </div>
          </div>

          {/* Venue (onsite only) */}
          {!isVirtual && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Venue</label>
              <input
                type="text"
                value={venue}
                onChange={(e) => setVenue(e.target.value)}
                placeholder="e.g. Bristol, NHS Wales offices"
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              />
            </div>
          )}

          {/* Visibility */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Visibility</label>
            <div className="flex rounded-lg border border-gray-200 overflow-hidden w-fit">
              <button
                type="button"
                onClick={() => setIsPrivate(false)}
                className={`px-4 py-2 text-sm font-medium transition-colors
                  ${!isPrivate ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
              >
                Public
              </button>
              <button
                type="button"
                onClick={() => setIsPrivate(true)}
                className={`px-4 py-2 text-sm font-medium border-l border-gray-200 transition-colors
                  ${isPrivate ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
              >
                Private
              </button>
            </div>
            {isPrivate && <p className="text-xs text-gray-400 mt-1">Private courses are created directly as live — no Planned stage.</p>}
          </div>

          {/* Decision Deadline — hidden for private courses */}
          {!isPrivate && (
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Decision Deadline</label>
              <input
                type="date"
                value={decisionDeadline}
                onChange={(e) => setDecisionDeadline(e.target.value)}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              />
              <p className="text-xs text-gray-400 mt-1">Defaults to 10 days before start date if left blank</p>
            </div>
          )}

          {/* Notes */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Notes</label>
            <textarea
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
              rows={3}
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              placeholder="Optional notes..."
            />
          </div>

          {/* Submit */}
          <div className="flex justify-end gap-2 pt-2 border-t border-gray-100">
            <Button type="button" variant="secondary" onClick={onClose}>Cancel</Button>
            <Button type="submit" disabled={submitting}>
              {submitting ? <><Loader2 className="w-4 h-4 animate-spin" /> {editMode ? "Updating..." : "Creating..."}</> : submitLabel}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ── Side Panel (course detail) ──────────────────────────────

interface SidePanelProps {
  event: CalendarEvent | null;
  onClose: () => void;
  onPublish: (eventId: string, gateway: string) => Promise<void>;
  onCancel: (eventId: string) => Promise<void>;
  onEdit?: (eventId: string) => void;
}

function SidePanel({ event, onClose, onPublish, onCancel, onEdit }: SidePanelProps) {
  const [publishing, setPublishing] = useState<string | null>(null);
  const [cancelling, setCancelling] = useState(false);
  const [confirmCancel, setConfirmCancel] = useState(false);

  if (!event) return null;

  const badgeSrc = getBadgeSrc(event.courseType);
  const displayName = getCourseDisplayName(event.courseType);
  const codeDisplay = getCourseCodeDisplay(event.courseType);
  const applicableGateways = getApplicableGateways(event.courseType, event.isPrivate);
  const isCancelled = event.status === "cancelled";

  // Date range display
  const startFormatted = formatDate(event.startDate);
  const endFormatted = event.startDate !== event.endDate ? formatDate(event.endDate) : null;
  const dateRange = endFormatted ? `${startFormatted} - ${endFormatted}` : startFormatted;

  // Enrolment bar (percentage)
  const fillPct = event.minimumEnrolments > 0
    ? Math.min(100, Math.round((event.enrolmentCount / event.minimumEnrolments) * 100))
    : 0;

  async function handlePublish(gateway: string) {
    setPublishing(gateway);
    try {
      await onPublish(event!.id, gateway);
    } finally {
      setPublishing(null);
    }
  }

  async function handleCancel() {
    setCancelling(true);
    try {
      await onCancel(event!.id);
      onClose();
    } finally {
      setCancelling(false);
      setConfirmCancel(false);
    }
  }

  const gatewayLabel = (type: string) => {
    switch (type) {
      case "ecommerce": return "E-commerce";
      case "scrumorg": return "Scrum.org";
      case "icagile": return "IC Agile";
      default: return type;
    }
  };

  const headerActions = event.status === "planned" && event.id.startsWith("planned-") && onEdit ? (
    <Button variant="secondary" size="sm" onClick={() => onEdit(event.id)}>
      Edit
    </Button>
  ) : undefined;

  return (
    <SlideOver open={!!event} onClose={onClose} title={codeDisplay} subtitle={displayName} actions={headerActions}>
      <div className="space-y-6">
        {/* Badge + dates */}
        <div className="flex items-start gap-4">
          {badgeSrc ? (
            <img src={badgeSrc} alt={event.courseType} className="w-12 h-12 rounded-lg object-contain" />
          ) : (
            <div className="w-12 h-12 rounded-lg bg-gray-100 text-gray-500 text-sm font-bold flex items-center justify-center">
              {event.courseType.slice(0, 3)}
            </div>
          )}
          <div>
            <p className="text-sm font-medium text-gray-900">{dateRange}</p>
            <p className="text-sm text-gray-500">
              {event.trainerName || event.trainerInitials}
              {event.isVirtual ? " (Virtual)" : event.venue ? ` (${event.venue})` : " (Onsite)"}
            </p>
            {event.isPrivate && (
              <span className="inline-flex items-center gap-1 mt-1 text-xs text-amber-700 bg-amber-50 px-2 py-0.5 rounded-full font-medium">
                <Lock className="w-3 h-3" /> Private
              </span>
            )}
          </div>
        </div>

        {/* Status badge */}
        <div>
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Status</p>
          <Badge variant={getStatusBadgeVariant(event.status)} dot>{getStatusLabel(event.status)}</Badge>
        </div>

        {/* Decision deadline */}
        {event.decisionDeadline && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Decision Deadline</p>
            <p className={`text-sm ${isDeadlineUrgent(event.decisionDeadline) ? "text-red-600 font-semibold" : "text-gray-700"}`}>
              {formatDate(event.decisionDeadline)}
              {isDeadlineUrgent(event.decisionDeadline) && (
                <span className="ml-2 inline-flex items-center gap-1 text-xs text-red-500">
                  <AlertCircle className="w-3 h-3" /> Approaching
                </span>
              )}
            </p>
          </div>
        )}

        {/* Enrolment bar */}
        {event.status !== "planned" && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Enrolment</p>
            <div className="flex items-center gap-2">
              <div className="flex-1 bg-gray-100 rounded-full h-2">
                <div
                  className={`h-2 rounded-full transition-all ${fillPct >= 100 ? "bg-green-500" : fillPct >= 50 ? "bg-amber-400" : "bg-red-400"}`}
                  style={{ width: `${fillPct}%` }}
                />
              </div>
              <span className="text-sm font-medium text-gray-700">{event.enrolmentCount}/{event.minimumEnrolments}</span>
            </div>
          </div>
        )}

        {/* Notes */}
        {event.notes && (
          <div>
            <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-1">Notes</p>
            <p className="text-sm text-gray-700 whitespace-pre-wrap">{event.notes}</p>
          </div>
        )}

        {/* Gateway checklist — hidden for private courses (no public gateways) */}
        {applicableGateways.length > 0 && (
        <div>
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Gateway Checklist</p>
          <div className="space-y-2">
            {applicableGateways.map((gwType) => {
              const gwStatus = event.gateways.find((g) => g.type === gwType);
              const isPublished = gwStatus?.published || false;
              const url = gwStatus?.url;
              const isIcAgile = gwType === "icagile";

              const rowContent = (
                <>
                  <div className="flex items-center gap-2">
                    {isPublished ? (
                      <span className="w-5 h-5 rounded-full bg-green-100 text-green-600 flex items-center justify-center text-xs font-bold">
                        &#10003;
                      </span>
                    ) : (
                      <span className="w-5 h-5 rounded-full bg-gray-100 text-gray-400 flex items-center justify-center text-xs">
                        &mdash;
                      </span>
                    )}
                    <span className="text-sm font-medium text-gray-700">{gatewayLabel(gwType)}</span>
                  </div>
                  <span className="flex items-center gap-1">
                    {isIcAgile ? (
                      <span className="text-xs text-gray-400 italic">Coming soon</span>
                    ) : isPublished && url ? (
                      <span className="text-xs text-brand-600 flex items-center gap-1 font-medium">
                        View <ExternalLink className="w-3 h-3" />
                      </span>
                    ) : !isPublished && !isCancelled ? (
                      <Button
                        size="sm"
                        onClick={(e) => { e.preventDefault(); handlePublish(gwType); }}
                        disabled={publishing === gwType}
                      >
                        {publishing === gwType ? (
                          <><Loader2 className="w-3 h-3 animate-spin" /> Publishing...</>
                        ) : (
                          "Publish →"
                        )}
                      </Button>
                    ) : null}
                  </span>
                </>
              );

              return isPublished && url ? (
                <a
                  key={gwType}
                  href={url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-100 hover:bg-green-50 hover:border-green-200 transition-colors cursor-pointer"
                >
                  {rowContent}
                </a>
              ) : (
                <div key={gwType} className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-100">
                  {rowContent}
                </div>
              );
            })}
          </div>
        </div>
        )}

        {/* External links */}
        <div>
          <p className="text-xs font-medium text-gray-500 uppercase tracking-wider mb-2">Links</p>
          <div className="space-y-1">
            {event.gateways.filter((g) => g.published && g.url).map((g) => (
              <a
                key={g.type}
                href={g.url!}
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-2 text-sm text-brand-600 hover:text-brand-700"
              >
                <ExternalLink className="w-3.5 h-3.5" />
                {gatewayLabel(g.type)} listing
              </a>
            ))}
            {/* Course management link — schedule-based courses (both public and private) */}
            {event.id.startsWith("schedule-") && (
              <a
                href={`/courses/${event.id.replace("schedule-", "")}`}
                className="flex items-center gap-2 text-sm text-brand-600 hover:text-brand-700"
              >
                <Users className="w-3.5 h-3.5" />
                {event.isPrivate ? "Manage course →" : "View attendees"}
              </a>
            )}
          </div>
        </div>

        {/* Cancel button */}
        {!isCancelled && (
          <div className="pt-4 border-t border-gray-100">
            {!confirmCancel ? (
              <Button variant="danger" size="sm" onClick={() => setConfirmCancel(true)}>
                Cancel Course
              </Button>
            ) : (
              <div className="p-3 bg-red-50 rounded-lg border border-red-200">
                <p className="text-sm text-red-700 mb-3">
                  Are you sure you want to cancel this course? This action cannot be undone.
                </p>
                <div className="flex gap-2">
                  <Button variant="danger" size="sm" onClick={handleCancel} disabled={cancelling}>
                    {cancelling ? "Cancelling..." : "Yes, Cancel"}
                  </Button>
                  <Button variant="secondary" size="sm" onClick={() => setConfirmCancel(false)}>
                    No, Keep It
                  </Button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </SlideOver>
  );
}

// ── Course List View ────────────────────────────────────────

function CourseListRow({ event, onClick }: { event: CalendarEvent; onClick: () => void }) {
  const badgeSrc = getBadgeSrc(event.courseType);
  const codeDisplay = getCourseCodeDisplay(event.courseType);
  const initials = event.trainerInitials || trainerInitials(event.trainerName);
  const avatarColour = getTrainerColour(initials);
  const deadlineUrgent = isDeadlineUrgent(event.decisionDeadline);
  const isCancelled = event.status === "cancelled";
  const applicableGateways = getApplicableGateways(event.courseType, event.isPrivate);

  const fmt = (d: string) =>
    new Date(d).toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" });
  const dateStr =
    event.startDate.split("T")[0] !== event.endDate.split("T")[0]
      ? `${fmt(event.startDate)} – ${fmt(event.endDate)}`
      : fmt(event.startDate);

  return (
    <tr
      onClick={onClick}
      className={`border-b border-gray-100 cursor-pointer transition-colors hover:bg-gray-50 ${isCancelled ? "opacity-50" : ""}`}
    >
      {/* Date */}
      <td className="px-4 py-3 w-52">
        <div className="flex items-center gap-2">
          {deadlineUrgent && (
            <span className="w-2 h-2 rounded-full bg-red-500 shrink-0" title="Decision deadline approaching" />
          )}
          <span className={`text-sm text-gray-700 whitespace-nowrap ${isCancelled ? "line-through" : ""}`}>
            {dateStr}
          </span>
        </div>
      </td>
      {/* Badge */}
      <td className="px-3 py-3 w-12">
        {badgeSrc ? (
          <img src={badgeSrc} alt={event.courseType} className="w-8 h-8 object-contain" />
        ) : (
          <div className="w-8 h-8 rounded bg-gray-100 flex items-center justify-center text-[9px] font-bold text-gray-500">
            {event.courseType.slice(0, 3)}
          </div>
        )}
      </td>
      {/* Course code */}
      <td className="px-3 py-3 min-w-[80px]">
        <div className="flex items-center gap-1.5">
          <span className="text-sm font-semibold text-gray-800">{codeDisplay}</span>
          {event.isPrivate && <Lock className="w-3 h-3 text-amber-500" />}
        </div>
      </td>
      {/* Trainer avatar */}
      <td className="px-3 py-3 w-12 hidden sm:table-cell">
        <span
          className="inline-flex items-center justify-center rounded-full text-white font-bold"
          style={{ backgroundColor: avatarColour, width: 24, height: 24, fontSize: 10 }}
        >
          {initials}
        </span>
      </td>
      {/* Status */}
      <td className="px-3 py-3">
        <Badge variant={getStatusBadgeVariant(event.status)}>{getStatusLabel(event.status)}</Badge>
      </td>
      {/* Enrolments */}
      <td className="px-3 py-3 text-sm text-gray-600 w-16 text-center hidden md:table-cell">
        {event.status === "planned" ? "–" : (
          <span className="flex items-center justify-center gap-0.5">
            <Users className="w-3 h-3 text-gray-400" />{event.enrolmentCount}
          </span>
        )}
      </td>
      {/* Gateways */}
      <td className="px-3 py-3 hidden md:table-cell">
        <div className="flex items-center gap-2">
          {applicableGateways.map((gw) => {
            const gwStatus = event.gateways?.find((g) => g.type === gw);
            const published = gwStatus?.published ?? false;
            const label = gw === "ecommerce" ? "E" : gw === "scrumorg" ? "S" : "I";
            return (
              <span key={gw} className="flex items-center gap-0.5" title={`${gw}: ${published ? "published" : "not published"}`}>
                <span className={`w-2 h-2 rounded-full ${published ? "bg-green-500" : "bg-gray-300"}`} />
                <span className="text-[10px] text-gray-400">{label}</span>
              </span>
            );
          })}
        </div>
      </td>
    </tr>
  );
}

function CourseListView({
  events,
  loading,
  onSelect,
}: {
  events: CalendarEvent[];
  loading: boolean;
  onSelect: (e: CalendarEvent) => void;
}) {
  if (loading) {
    return (
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-12 flex items-center justify-center">
        <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
      </div>
    );
  }

  if (events.length === 0) {
    return (
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-12 text-center text-gray-500 text-sm">
        No courses found.
      </div>
    );
  }

  // Group by month
  const sorted = [...events].sort((a, b) => a.startDate.localeCompare(b.startDate));
  const grouped: { month: string; label: string; events: CalendarEvent[] }[] = [];
  for (const e of sorted) {
    const month = e.startDate.substring(0, 7);
    let group = grouped.find((g) => g.month === month);
    if (!group) {
      group = {
        month,
        label: new Date(month + "-01").toLocaleString("en-GB", { month: "long", year: "numeric" }),
        events: [],
      };
      grouped.push(group);
    }
    group.events.push(e);
  }

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="overflow-x-auto">
      <table className="w-full">
        <thead>
          <tr className="border-b border-gray-200 bg-gray-50">
            <th className="px-4 py-2.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Date</th>
            <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider" colSpan={2}>Course</th>
            <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider hidden sm:table-cell">Trainer</th>
            <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider">Status</th>
            <th className="px-3 py-2.5 text-center text-xs font-semibold text-gray-500 uppercase tracking-wider hidden md:table-cell">Enrols</th>
            <th className="px-3 py-2.5 text-left text-xs font-semibold text-gray-500 uppercase tracking-wider hidden md:table-cell">Gateways</th>
          </tr>
        </thead>
        {grouped.map(({ month, label, events: monthEvents }) => (
          <tbody key={month}>
            <tr className="bg-gray-50 border-t border-b border-gray-200">
              <td colSpan={7} className="px-4 py-2">
                <span className="text-xs font-semibold text-gray-600 uppercase tracking-wider">{label}</span>
              </td>
            </tr>
            {monthEvents.map((e) => (
              <CourseListRow key={e.id} event={e} onClick={() => onSelect(e)} />
            ))}
          </tbody>
        ))}
      </table>
      </div>
    </div>
  );
}

// ── Main Calendar Page ──────────────────────────────────────

function CalendarContent() {
  const apiKey = useApiKey();
  const calendarRef = useRef<FullCalendar>(null);
  const searchParams = useSearchParams();
  const [events, setEvents] = useState<CalendarEvent[]>([]);
  const [trainers, setTrainers] = useState<Trainer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [trainerFilter, setTrainerFilter] = useState<string>("all");
  const [statusFilter, setStatusFilter] = useState<string>("all");
  const [typeFilter, setTypeFilter] = useState<"all" | "public" | "private">("all");
  const [viewMode, setViewMode] = useState<"calendar" | "list">(
    searchParams.get("view") === "calendar" ? "calendar" : "list"
  );
  const [listEvents, setListEvents] = useState<CalendarEvent[]>([]);
  const [listLoading, setListLoading] = useState(false);
  const [listError, setListError] = useState("");
  const [selectedEvent, setSelectedEvent] = useState<CalendarEvent | null>(null);
  const [showAddModal, setShowAddModal] = useState(false);
  const [currentRange, setCurrentRange] = useState<{ from: string; to: string } | null>(null);
  const [toast, setToast] = useState<{ message: string; type: "success" | "error" } | null>(null);
  const [editCourseId, setEditCourseId] = useState<number | null>(null);
  const [editInitialValues, setEditInitialValues] = useState<AddCourseModalProps["initialValues"]>(undefined);
  const [listSearch, setListSearch] = useState("");
  const [listDateRange, setListDateRange] = useState<"upcoming" | "year" | "all">("upcoming");
  const [showImportModal, setShowImportModal] = useState(false);

  // Load trainers on mount
  useEffect(() => {
    if (!apiKey) return;
    getTrainers(apiKey).then(setTrainers).catch(() => {});
  }, [apiKey]);

  // Load list events when switching to list view or date range changes
  useEffect(() => {
    if (viewMode !== "list" || !apiKey) return;
    setListLoading(true);
    const today = new Date().toISOString().split("T")[0];
    let from: string;
    let to: string;
    if (listDateRange === "upcoming") {
      from = today;
      to = new Date(Date.now() + 366 * 24 * 3600 * 1000).toISOString().split("T")[0];
    } else if (listDateRange === "year") {
      const y = new Date().getFullYear();
      from = `${y}-01-01`;
      to = `${y}-12-31`;
    } else {
      from = "2020-01-01";
      to = "2030-12-31";
    }
    setListError("");
    getCalendarEvents(apiKey, from, to)
      .then(setListEvents)
      .catch((err) => setListError(err instanceof Error ? err.message : "Failed to load courses"))
      .finally(() => setListLoading(false));
  }, [viewMode, apiKey, listDateRange]);

  // Load calendar events when range changes
  const loadEvents = useCallback(async () => {
    if (!apiKey || !currentRange) {
      setLoading(false);
      return;
    }
    setLoading(true);
    setError("");
    try {
      const data = await getCalendarEvents(apiKey, currentRange.from, currentRange.to);
      setEvents(data);
    } catch {
      setError("Failed to load calendar events");
    } finally {
      setLoading(false);
    }
  }, [apiKey, currentRange]);

  useEffect(() => { loadEvents(); }, [loadEvents]);

  // Filter events by trainer + status + type (client-side)
  const applyFilters = (source: CalendarEvent[]) =>
    source.filter((e) => {
      if (trainerFilter !== "all") {
        const initials = e.trainerInitials || trainerInitials(e.trainerName);
        if (initials !== trainerFilter) return false;
      }
      if (statusFilter !== "all" && e.status !== statusFilter) return false;
      if (typeFilter === "public" && e.isPrivate) return false;
      if (typeFilter === "private" && !e.isPrivate) return false;
      return true;
    });

  const filteredEvents = applyFilters(events);
  const filteredListEvents = applyFilters(listEvents);

  // Search filter on top of trainer/status filters
  const filteredListEventsSearched = filteredListEvents.filter((e) => {
    if (!listSearch) return true;
    const q = listSearch.toLowerCase();
    return (
      e.courseType.toLowerCase().includes(q) ||
      getCourseDisplayName(e.courseType).toLowerCase().includes(q) ||
      getCourseCodeDisplay(e.courseType).toLowerCase().includes(q)
    );
  });

  // Convert to FullCalendar events
  const fcEvents = filteredEvents.map((e) => ({
    id: e.id,
    start: e.startDate.split("T")[0],
    // FullCalendar dayGrid end is exclusive, so add 1 day
    end: addOneDayStr(e.endDate.split("T")[0]),
    extendedProps: { calendarEvent: e } as FCEventExtended,
    display: "block" as const,
    backgroundColor: "transparent",
    borderColor: "transparent",
  }));

  // Handlers
  async function handleAddCourse(data: Parameters<AddCourseModalProps["onSubmit"]>[0]) {
    if (!apiKey) return;
    if (data.isPrivate) {
      const trainer = trainers.find((t) => t.id === data.trainerId);
      const formatType = data.isVirtual ? "virtual" : "f2f";
      await createPrivateCourse(apiKey, {
        name: generateCourseName(data.courseType, "", formatType),
        courseCode: data.courseType,
        startDate: data.startDate,
        endDate: data.endDate,
        formatType,
        trainerName: trainer?.name,
        venueAddress: data.venue,
        notes: data.notes,
      });
    } else {
      await createPlannedCourse(apiKey, data);
    }
    setToast({ message: "Course created", type: "success" });
    await loadEvents();
  }

  async function handleEditCourse(data: Parameters<AddCourseModalProps["onSubmit"]>[0]) {
    if (!apiKey || !editCourseId) return;
    await updatePlannedCourse(apiKey, editCourseId, data);
    setToast({ message: "Course updated", type: "success" });
    await loadEvents();
  }

  function handleModalClose() {
    setShowAddModal(false);
    setEditCourseId(null);
    setEditInitialValues(undefined);
  }

  function handleOpenEdit(eventId: string) {
    const event = events.find((e) => e.id === eventId) ?? listEvents.find((e) => e.id === eventId);
    if (!event) return;
    const numericId = Number(eventId.replace(/\D/g, ""));
    const trainer = trainers.find((t) => t.name === event.trainerName);
    setEditCourseId(numericId);
    setEditInitialValues({
      courseType: event.courseType,
      trainerId: trainer?.id ?? trainers[0]?.id ?? 0,
      startDate: event.startDate.split("T")[0],
      endDate: event.endDate.split("T")[0],
      isVirtual: event.isVirtual,
      venue: event.venue ?? "",
      notes: event.notes ?? "",
      decisionDeadline: event.decisionDeadline ?? "",
      isPrivate: event.isPrivate,
    });
    setShowAddModal(true);
  }

  function handleExportCsv() {
    const source = viewMode === "list" ? filteredListEventsSearched : filteredEvents;
    const header = "courseType,startDate,endDate,trainer,isVirtual,venue,notes";
    const rows = source.map((e) => [
      e.courseType,
      e.startDate.split("T")[0],
      e.endDate.split("T")[0],
      e.trainerName ?? e.trainerInitials ?? "",
      e.isVirtual ? "true" : "false",
      e.venue ?? "",
      (e.notes ?? "").replace(/,/g, ";"),
    ].join(","));
    const csv = [header, ...rows].join("\n");
    const blob = new Blob([csv], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `courses-${new Date().toISOString().split("T")[0]}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }

  async function handlePublish(eventId: string, gateway: string) {
    if (!apiKey) return;
    try {
      await publishGateway(apiKey, eventId, gateway);
      setToast({ message: `Published to ${gateway}`, type: "success" });
      await loadEvents();
      // Refresh selected event if it's the same one
      if (selectedEvent?.id === eventId) {
        const refreshed = events.find((e) => e.id === eventId);
        if (refreshed) setSelectedEvent(refreshed);
      }
    } catch (err) {
      setToast({ message: err instanceof Error ? err.message : "Publish failed", type: "error" });
    }
  }

  async function handleCancel(eventId: string) {
    if (!apiKey) return;
    const numericId = eventId.replace(/\D/g, "");
    try {
      await updatePlannedCourse(apiKey, Number(numericId), { status: "cancelled" });
      setToast({ message: "Course cancelled", type: "success" });
      await loadEvents();
    } catch (err) {
      setToast({ message: err instanceof Error ? err.message : "Cancel failed", type: "error" });
    }
  }

  function handleDatesSet(info: { startStr: string; endStr: string }) {
    // FullCalendar passes ISO strings; extract date portion
    const from = info.startStr.split("T")[0];
    const to = info.endStr.split("T")[0];
    setCurrentRange({ from, to });
  }

  // After loadEvents completes, sync selectedEvent with latest data
  useEffect(() => {
    if (selectedEvent) {
      const updated = events.find((e) => e.id === selectedEvent.id);
      if (updated) setSelectedEvent(updated);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [events]);

  return (
    <>
      <PageHeader
        title="Course Schedule"
        actions={
          <div className="flex items-center gap-2">
            <Button variant="secondary" size="sm" onClick={() => setShowImportModal(true)} title="Import CSV">
              <Upload className="w-4 h-4" /> <span className="hidden sm:inline">Import CSV</span>
            </Button>
            <Button variant="secondary" size="sm" onClick={handleExportCsv} title="Export CSV">
              <Download className="w-4 h-4" /> <span className="hidden sm:inline">Export CSV</span>
            </Button>
            <Button size="sm" onClick={() => setShowAddModal(true)}>
              <Plus className="w-4 h-4" /> Add Course
            </Button>
          </div>
        }
      />

      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

      {/* Toolbar */}
      <div className="flex items-center justify-between gap-4 mb-4 flex-wrap">
        {/* Left: view toggle + trainer filter */}
        <div className="flex items-center gap-3 flex-wrap">
          {/* View toggle */}
          <div className="flex rounded-lg border border-gray-200 overflow-hidden">
            {([{ key: "list", icon: <LayoutList className="w-3.5 h-3.5" />, label: "List" }, { key: "calendar", icon: <CalendarIcon className="w-3.5 h-3.5" />, label: "Calendar" }] as const).map((v, i) => (
              <button
                key={v.key}
                onClick={() => setViewMode(v.key)}
                className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium transition-colors
                  ${viewMode === v.key ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}
                  ${i > 0 ? "border-l border-gray-200" : ""}`}
              >
                {v.icon}{v.label}
              </button>
            ))}
          </div>
          {/* Trainer filter — dropdown scales to any number of trainers */}
          <select
            value={trainerFilter}
            onChange={(e) => setTrainerFilter(e.target.value)}
            className="px-3 py-1.5 text-xs font-medium border border-gray-200 rounded-lg bg-white text-gray-600 focus:outline-none focus:ring-1 focus:ring-brand-500"
          >
            <option value="all">All trainers</option>
            {trainers.map((t) => (
              <option key={t.id} value={trainerInitials(t.name)}>{t.name}</option>
            ))}
          </select>

          {/* Type filter — public / private */}
          <div className="flex rounded-lg border border-gray-200 overflow-hidden">
            {(["all", "public", "private"] as const).map((key, i) => (
              <button
                key={key}
                onClick={() => setTypeFilter(key)}
                className={`px-3 py-1.5 text-xs font-medium capitalize transition-colors
                  ${typeFilter === key ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}
                  ${i > 0 ? "border-l border-gray-200" : ""}`}
              >
                {key === "all" ? "All types" : key}
              </button>
            ))}
          </div>
          {(loading || listLoading) && <span className="text-xs text-gray-400">Loading...</span>}
        </div>

        {/* Right: status filter (doubles as legend) */}
        <div className="flex items-center gap-2 flex-wrap">
          <button
            onClick={() => setStatusFilter("all")}
            className={`text-xs px-2.5 py-1 rounded-full border font-medium transition-colors
              ${statusFilter === "all" ? "bg-brand-600 text-white border-brand-600" : "bg-white text-gray-500 border-gray-200 hover:border-gray-300"}`}
          >
            All
          </button>
          {[
            { key: "planned", colour: "#9ca3af", label: "Planned" },
            { key: "partial_live", colour: "#f59e0b", label: "Partial live" },
            { key: "live", colour: "#22c55e", label: "Live" },
            { key: "cancelled", colour: "#ef4444", label: "Cancelled" },
          ].map(({ key, colour, label }) => (
            <button
              key={key}
              onClick={() => setStatusFilter(statusFilter === key ? "all" : key)}
              className={`flex items-center gap-1.5 text-xs px-2.5 py-1 rounded-full border font-medium transition-colors
                ${statusFilter === key ? "bg-white font-semibold" : "bg-white text-gray-500 border-gray-200 hover:border-gray-300"}`}
              style={statusFilter === key ? { color: colour, borderColor: colour } : {}}
            >
              <span className="w-2.5 h-2.5 rounded-sm shrink-0" style={{ backgroundColor: colour }} />
              {label}
            </button>
          ))}
          <div className="flex items-center gap-1.5 ml-1">
            <span className="w-2 h-2 rounded-full bg-red-500 shrink-0" />
            <span className="text-xs text-gray-400">Decision due</span>
          </div>
        </div>
      </div>

      {/* Calendar or List view */}
      {viewMode === "calendar" ? (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden calendar-container">
          <FullCalendar
            ref={calendarRef}
            plugins={[dayGridPlugin, timeGridPlugin]}
            initialView="dayGridMonth"
            headerToolbar={{
              left: "prev,next today",
              center: "title",
              right: "dayGridMonth,timeGridWeek",
            }}
            events={fcEvents}
            datesSet={handleDatesSet}
            eventClick={(info) => {
              const ext = info.event.extendedProps as FCEventExtended;
              setSelectedEvent(ext.calendarEvent);
            }}
            eventContent={(arg) => {
              const ext = arg.event.extendedProps as FCEventExtended;
              return <CourseBlock event={ext.calendarEvent} />;
            }}
            height="auto"
            fixedWeekCount={false}
            firstDay={1}
            dayMaxEvents={4}
          />
        </div>
      ) : (
        <>
          {/* List view filters */}
          <div className="flex items-center gap-3 mb-4 flex-wrap">
            <div className="relative flex-1 min-w-[180px] max-w-xs">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                placeholder="Search courses..."
                value={listSearch}
                onChange={(e) => setListSearch(e.target.value)}
                className="w-full pl-10 pr-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              />
            </div>
            <div className="inline-flex rounded-lg border border-gray-200 overflow-hidden">
              {([
                { key: "upcoming", label: "Upcoming" },
                { key: "year", label: String(new Date().getFullYear()) },
                { key: "all", label: "All" },
              ] as const).map(({ key, label }, i) => (
                <button
                  key={key}
                  onClick={() => setListDateRange(key)}
                  className={`px-3 py-1.5 text-xs font-medium transition-colors
                    ${listDateRange === key ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}
                    ${i > 0 ? "border-l border-gray-200" : ""}`}
                >
                  {label}
                </button>
              ))}
            </div>
          </div>
          {listError && (
            <div className="mb-4">
              <AlertBanner variant="danger">{listError}</AlertBanner>
            </div>
          )}
          <CourseListView
            events={filteredListEventsSearched}
            loading={listLoading}
            onSelect={setSelectedEvent}
          />
        </>
      )}

      {/* Side panel */}
      <SidePanel
        event={selectedEvent}
        onClose={() => setSelectedEvent(null)}
        onPublish={handlePublish}
        onCancel={handleCancel}
        onEdit={handleOpenEdit}
      />

      {/* Add / Edit course modal */}
      <AddCourseModal
        open={showAddModal}
        onClose={handleModalClose}
        onSubmit={editCourseId ? handleEditCourse : handleAddCourse}
        trainers={trainers}
        initialValues={editInitialValues}
        editMode={!!editCourseId}
      />

      {/* Toast */}
      <CsvImportModal
        open={showImportModal}
        onClose={() => setShowImportModal(false)}
        onImported={async () => { await loadEvents(); setToast({ message: "Courses imported", type: "success" }); }}
        trainers={trainers}
        apiKey={apiKey ?? ""}
      />

      {toast && <Toast message={toast.message} type={toast.type} onClose={() => setToast(null)} />}

      {/* Calendar CSS overrides */}
      <style>{`
        .calendar-container .fc {
          font-family: var(--font-sans);
        }
        .calendar-container .fc .fc-toolbar-title {
          font-size: 1rem;
          font-weight: 600;
          color: #111827;
        }
        .calendar-container .fc .fc-button {
          background: white;
          border: 1px solid #e5e7eb;
          color: #374151;
          font-size: 0.75rem;
          font-weight: 500;
          padding: 0.375rem 0.75rem;
          border-radius: 0.5rem;
          box-shadow: none;
        }
        .calendar-container .fc .fc-button:hover {
          background: #f9fafb;
        }
        .calendar-container .fc .fc-button-active {
          background: #003366 !important;
          color: white !important;
          border-color: #003366 !important;
        }
        .calendar-container .fc .fc-button:focus {
          box-shadow: 0 0 0 2px rgba(0, 51, 102, 0.3);
        }
        .calendar-container .fc .fc-col-header-cell {
          background: #f9fafb;
          font-size: 0.75rem;
          font-weight: 500;
          color: #6b7280;
          text-transform: uppercase;
          padding: 0.5rem 0;
        }
        .calendar-container .fc .fc-daygrid-day-number {
          font-size: 0.75rem;
          font-weight: 500;
          color: #6b7280;
          padding: 0.25rem 0.5rem;
        }
        .calendar-container .fc .fc-day-today {
          background: #eff6ff !important;
        }
        .calendar-container .fc .fc-day-today .fc-daygrid-day-number {
          background: #2563eb;
          color: white;
          border-radius: 9999px;
          width: 1.5rem;
          height: 1.5rem;
          display: flex;
          align-items: center;
          justify-content: center;
        }
        .calendar-container .fc .fc-daygrid-day-frame {
          min-height: 90px;
        }
        .calendar-container .fc .fc-event {
          margin: 1px 2px;
          border-radius: 0.25rem;
        }
        .calendar-container .fc .fc-daygrid-more-link {
          font-size: 0.625rem;
          color: #6b7280;
          font-weight: 500;
        }
        .calendar-container .fc .fc-toolbar {
          padding: 0.75rem 1rem;
        }
        .calendar-container .fc .fc-prev-button,
        .calendar-container .fc .fc-next-button {
          padding: 0.25rem 0.5rem;
        }
      `}</style>
    </>
  );
}

export default function CalendarPage() {
  return <Suspense><CalendarContent /></Suspense>;
}
