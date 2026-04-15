"use client";

import { useEffect, useState } from "react";
import { Lock, Plus, CalendarDays } from "lucide-react";
import Link from "next/link";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  CourseScheduleItem,
  getPrivateCourses,
  patchCourseStatus,
  formatDate,
} from "@/lib/api";
import { getBadgeSrc, getCourseCodeDisplay, extractCourseTypeFromSku, getCourseDisplayName, getStatusLabel, getStatusBadgeVariant } from "@/lib/calendarHelpers";
import { PageHeader } from "@/components/ui/PageHeader";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { CreatePrivateCoursePanel } from "@/components/courses/CreatePrivateCoursePanel";

const PRIVATE_STATUSES = ["enquiry", "quoted", "confirmed", "completed", "cancelled"] as const;

// ── Date range helpers ───────────────────────────────────────────────────────

function toIsoDate(d: Date): string {
  return d.toISOString().slice(0, 10);
}

function defaultRange(): { from: string; to: string } {
  const today = new Date();
  const future = new Date(today);
  future.setFullYear(future.getFullYear() + 1);
  return { from: toIsoDate(today), to: toIsoDate(future) };
}

// ── Row component ────────────────────────────────────────────────────────────

function CourseRow({ course, apiKey }: { course: CourseScheduleItem; apiKey: string }) {
  const courseType = extractCourseTypeFromSku(course.courseCode);
  const badgeSrc = getBadgeSrc(courseType);
  const codeDisplay = getCourseCodeDisplay(courseType);
  const courseName = getCourseDisplayName(courseType);
  const [status, setStatus] = useState(course.status ?? "confirmed");
  const [saving, setSaving] = useState(false);
  const [editing, setEditing] = useState(false);

  async function handleStatusChange(newStatus: string) {
    if (newStatus === status) { setEditing(false); return; }
    setSaving(true);
    try {
      await patchCourseStatus(apiKey, course.id, newStatus);
      setStatus(newStatus);
    } finally {
      setSaving(false);
      setEditing(false);
    }
  }

  return (
    <tr
      className="border-b border-gray-100 hover:bg-gray-50 transition-colors cursor-pointer"
      onClick={() => { window.location.href = `/courses/${course.id}`; }}
    >
      <td className="px-4 py-3">
        <div className="flex items-center gap-3">
          {badgeSrc ? (
            <img src={badgeSrc} alt={codeDisplay} className="h-8 w-8 object-contain shrink-0" />
          ) : (
            <div className="h-8 w-8 rounded bg-gray-200 shrink-0" />
          )}
          <div>
            <div className="text-sm font-medium text-gray-800">{codeDisplay}</div>
            <div className="text-xs text-gray-500">{courseName !== courseType ? courseName : ""}</div>
          </div>
        </div>
      </td>
      <td className="px-4 py-3 text-sm text-gray-700">
        {course.clientOrganisationName ?? "—"}
      </td>
      <td className="px-4 py-3 text-sm text-gray-600">
        {formatDate(course.startDate)}
        {course.endDate && course.endDate !== course.startDate
          ? ` – ${formatDate(course.endDate)}`
          : ""}
      </td>
      <td className="px-4 py-3 text-sm text-gray-600">
        {course.trainerName ?? "—"}
      </td>
      <td className="px-4 py-3 text-sm text-gray-600">
        {course.currentEnrolmentCount}
        {course.capacity != null ? ` / ${course.capacity}` : ""}
      </td>
      <td className="px-4 py-3" onClick={(e) => { e.stopPropagation(); setEditing(true); }}>
        {editing ? (
          <select
            autoFocus
            value={status}
            disabled={saving}
            onChange={(e) => handleStatusChange(e.target.value)}
            onBlur={() => setEditing(false)}
            className="text-xs border border-gray-300 rounded px-2 py-1 bg-white focus:outline-none focus:ring-1 focus:ring-brand-500"
          >
            {PRIVATE_STATUSES.map((s) => (
              <option key={s} value={s}>{getStatusLabel(s)}</option>
            ))}
          </select>
        ) : (
          <span title="Click to change status">
            <Badge variant={getStatusBadgeVariant(status)} dot>
              {saving ? "…" : getStatusLabel(status)}
            </Badge>
          </span>
        )}
      </td>
    </tr>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────

export default function PrivateCoursesPage() {
  const apiKey = useApiKey() ?? "";
  const [courses, setCourses] = useState<CourseScheduleItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");
  const [showAll, setShowAll] = useState(false);
  const [showCreate, setShowCreate] = useState(false);

  useEffect(() => {
    if (!apiKey) return;
    setLoading(true);
    setError("");
    const range = showAll ? {} : defaultRange();
    getPrivateCourses(apiKey, range)
      .then((res) => setCourses(res.items))
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [apiKey, showAll]);

  return (
    <div className="p-6 max-w-6xl mx-auto">
      {apiKey && (
        <CreatePrivateCoursePanel
          open={showCreate}
          onClose={() => setShowCreate(false)}
          apiKey={apiKey}
          onCreated={() => setShowCreate(false)}
        />
      )}

      <PageHeader
        title="Private Courses"
        subtitle="Company-booked courses managed outside the public schedule"
        actions={
          <div className="flex items-center gap-2">
            <Link
              href="/courseschedule?type=private"
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-gray-600 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 transition-colors"
            >
              <CalendarDays className="w-4 h-4" /> Calendar view
            </Link>
            <Button
              variant={showAll ? "primary" : "secondary"}
              size="sm"
              onClick={() => setShowAll((v) => !v)}
            >
              {showAll ? "Showing all" : "Show all dates"}
            </Button>
            <Button size="sm" onClick={() => setShowCreate(true)}>
              <Plus className="w-4 h-4" /> Create Private Course
            </Button>
          </div>
        }
      />

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left">
            <thead>
              <tr className="border-b border-gray-200 text-xs font-semibold uppercase tracking-wider text-gray-500">
                <th className="px-4 py-3">Course</th>
                <th className="px-4 py-3">Client</th>
                <th className="px-4 py-3">Dates</th>
                <th className="px-4 py-3">Trainer</th>
                <th className="px-4 py-3">Attendees</th>
                <th className="px-4 py-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <>
                  <SkeletonRow cols={6} />
                  <SkeletonRow cols={6} />
                  <SkeletonRow cols={6} />
                </>
              )}
              {!loading && courses.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-12 text-center">
                    <div className="flex flex-col items-center gap-2 text-gray-400">
                      <Lock className="w-8 h-8" />
                      <p className="text-sm">No private courses found for this period.</p>
                      {!showAll && (
                        <button
                          onClick={() => setShowAll(true)}
                          className="text-sm text-accent hover:underline mt-1"
                        >
                          Show all dates
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              )}
              {!loading &&
                courses.map((c) => <CourseRow key={c.id} course={c} apiKey={apiKey} />)}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
