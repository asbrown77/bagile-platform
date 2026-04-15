"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { Lock, Plus } from "lucide-react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  CourseScheduleItem,
  getPrivateCourses,
  formatDate,
} from "@/lib/api";
import { getBadgeSrc, getCourseCodeDisplay, extractCourseTypeFromSku, getCourseDisplayName } from "@/lib/calendarHelpers";
import { PageHeader } from "@/components/ui/PageHeader";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { CreatePrivateCoursePanel } from "@/components/courses/CreatePrivateCoursePanel";

// ── Status badge mapping for course schedule statuses ───────────────────────

function scheduleStatusBadge(status: string | null) {
  if (!status) return <Badge variant="neutral">Unknown</Badge>;
  const map: Record<string, { label: string; variant: "success" | "info" | "warning" | "danger" | "neutral" }> = {
    confirmed:    { label: "Confirmed",    variant: "info" },
    planned:      { label: "Planned",      variant: "neutral" },
    partial_live: { label: "Partial Live", variant: "warning" },
    live:         { label: "Live",         variant: "success" },
    cancelled:    { label: "Cancelled",    variant: "danger" },
  };
  const entry = map[status.toLowerCase()] ?? { label: status, variant: "neutral" as const };
  return <Badge variant={entry.variant} dot>{entry.label}</Badge>;
}

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

function CourseRow({ course }: { course: CourseScheduleItem }) {
  const courseType = extractCourseTypeFromSku(course.courseCode);
  const badgeSrc = getBadgeSrc(courseType);
  const codeDisplay = getCourseCodeDisplay(courseType);
  const courseName = getCourseDisplayName(courseType);

  return (
    <tr className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
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
      <td className="px-4 py-3">
        {scheduleStatusBadge(course.status)}
      </td>
      <td className="px-4 py-3 text-right">
        <Link
          href={`/courses/${course.id}`}
          className="text-sm font-medium text-accent hover:underline"
        >
          Manage →
        </Link>
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
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody>
              {loading && (
                <>
                  <SkeletonRow cols={7} />
                  <SkeletonRow cols={7} />
                  <SkeletonRow cols={7} />
                </>
              )}
              {!loading && courses.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-4 py-12 text-center">
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
                courses.map((c) => <CourseRow key={c.id} course={c} />)}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
