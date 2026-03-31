"use client";

import { Suspense, useEffect, useState, useCallback } from "react";
import { useSearchParams } from "next/navigation";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { CourseScheduleItem, getCourseSchedules, formatDate } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { Badge, statusBadge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { CreatePrivateCoursePanel } from "@/components/courses/CreatePrivateCoursePanel";
import { GraduationCap, Plus } from "lucide-react";

export default function CoursesPage() {
  return <Suspense><CoursesContent /></Suspense>;
}

function CoursesContent() {
  const apiKey = useApiKey();
  const searchParams = useSearchParams();

  const urlType = searchParams.get("type") || "";
  const urlYear = searchParams.get("year") || "";

  const currentYear = new Date().getFullYear();

  const [courses, setCourses] = useState<CourseScheduleItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [typeFilter, setTypeFilter] = useState(urlType || "all");
  const [dateRange, setDateRange] = useState<"upcoming" | "year" | "all">(
    urlType ? "year" : "upcoming"
  );
  const [year, setYear] = useState(urlYear ? Number(urlYear) : currentYear);
  const [showCreate, setShowCreate] = useState(false);
  const [visibilityFilter, setVisibilityFilter] = useState("all");

  const loadCourses = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);

    let from: string | undefined;
    let to: string | undefined;

    if (dateRange === "upcoming") {
      const d = new Date(); d.setDate(d.getDate() - 2);
      from = d.toISOString().split("T")[0];
    } else if (dateRange === "year") {
      from = `${year}-01-01`;
      to = `${year}-12-31`;
    }

    const courseCode = typeFilter !== "all" ? typeFilter : undefined;
    const type = visibilityFilter !== "all" ? visibilityFilter : undefined;

    try {
      const result = await getCourseSchedules(apiKey, {
        from, to, courseCode, type, pageSize: 100,
      });
      setCourses((result.items || []).filter(
        (c) => c.title && c.title !== "N/A" && c.courseCode
      ));
    } catch {
      setCourses([]);
    } finally {
      setLoading(false);
    }
  }, [apiKey, dateRange, year, typeFilter, visibilityFilter]);

  useEffect(() => { loadCourses(); }, [loadCourses]);

  const courseTypes = [...new Set(
    courses.map((c) => c.courseCode?.split("-")[0]).filter(Boolean)
  )].sort() as string[];

  const today = new Date(); today.setHours(0, 0, 0, 0);
  const getDisplayStatus = (c: CourseScheduleItem) => {
    if (c.status === "cancelled") return "cancelled";
    const start = new Date(c.startDate || ""); start.setHours(0, 0, 0, 0);
    const end = c.endDate ? new Date(c.endDate) : start; end.setHours(0, 0, 0, 0);
    if (start <= today && today <= end) return "running";
    if (today > end) return "completed";
    if (c.needsAttention) return "at risk";
    return c.guaranteedToRun ? "guaranteed" : "monitor";
  };

  const isVirtual = (c: CourseScheduleItem) =>
    c.formatType?.toLowerCase().includes("virtual") || c.location?.toLowerCase().includes("virtual");

  const totalAttendees = courses.reduce((s, c) => s + c.currentEnrolmentCount, 0);
  const completedCount = courses.filter((c) => getDisplayStatus(c) === "completed").length;

  return (
    <>
      <CreatePrivateCoursePanel
        open={showCreate}
        onClose={() => setShowCreate(false)}
        apiKey={apiKey}
        onCreated={loadCourses}
      />

      <PageHeader
        title="Courses"
        subtitle={typeFilter !== "all"
          ? `${typeFilter} — ${courses.length} courses, ${totalAttendees} total attendees`
          : `${courses.length} courses`}
        actions={
          <Button size="sm" onClick={() => setShowCreate(true)}>
            <Plus className="w-3.5 h-3.5" /> Create Private Course
          </Button>
        }
      />

      {/* Filters */}
      <div className="flex gap-3 mb-4 items-center flex-wrap">
        <select value={dateRange} onChange={(e) => setDateRange(e.target.value as "upcoming" | "year" | "all")}
          className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
          <option value="upcoming">Upcoming</option>
          <option value="year">Full Year</option>
          <option value="all">All Time</option>
        </select>

        {dateRange === "year" && (
          <select value={year} onChange={(e) => setYear(Number(e.target.value))}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
            {[2026, 2025, 2024].map((y) => <option key={y} value={y}>{y}</option>)}
          </select>
        )}

        <select value={visibilityFilter} onChange={(e) => setVisibilityFilter(e.target.value)}
          className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
          <option value="all">Public & Private</option>
          <option value="public">Public only</option>
          <option value="private">Private only</option>
        </select>

        <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}
          className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
          <option value="all">All types</option>
          {urlType && !courseTypes.includes(urlType) && <option value={urlType}>{urlType}</option>}
          {courseTypes.map((t) => <option key={t} value={t}>{t}</option>)}
        </select>

        <span className="text-xs text-gray-400">
          {courses.length} course{courses.length !== 1 ? "s" : ""}
          {completedCount > 0 && ` (${completedCount} completed)`}
        </span>
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Course</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Date</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Trainer</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Enrolled</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden lg:table-cell">Format</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 8 }).map((_, i) => <SkeletonRow key={i} cols={6} />)}
            {!loading && courses.map((c) => {
              const status = getDisplayStatus(c);
              const daysAway = c.startDate
                ? Math.round((new Date(c.startDate).getTime() - today.getTime()) / (1000 * 60 * 60 * 24))
                : 0;
              return (
                <tr key={c.id}
                  className={`border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors
                    ${status === "at risk" ? "bg-red-50/40" : ""}
                    ${status === "completed" ? "opacity-70" : ""}`}
                  onClick={() => window.location.href = `/courses/${c.id}`}>
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-900">{c.title}</p>
                    <p className="text-xs text-gray-400 font-mono">{c.courseCode}</p>
                  </td>
                  <td className="px-4 py-3 text-gray-600">
                    {formatDate(c.startDate)}
                    <p className="text-xs text-gray-400">
                      {daysAway > 0 ? `${daysAway}d away` : daysAway === 0 ? "Today" : `${Math.abs(daysAway)}d ago`}
                    </p>
                  </td>
                  <td className="px-4 py-3 text-gray-600 hidden md:table-cell">{c.trainerName || "—"}</td>
                  <td className="px-4 py-3 text-center">
                    <span className={`text-lg font-bold ${
                      status === "at risk" ? "text-red-600" :
                      status === "guaranteed" || status === "running" || status === "completed" ? "text-green-600" :
                      "text-gray-700"
                    }`}>
                      {c.currentEnrolmentCount}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center">{statusBadge(status)}</td>
                  <td className="px-4 py-3 text-center hidden lg:table-cell">
                    <div className="flex flex-col items-center gap-1">
                      <Badge variant={isVirtual(c) ? "info" : "neutral"} dot>
                        {isVirtual(c) ? "Virtual" : "In-person"}
                      </Badge>
                      {c.type && (
                        <Badge variant={c.type === "public" ? "success" : "warning"} dot>
                          {c.type === "public" ? "Public" : "Private"}
                        </Badge>
                      )}
                    </div>
                  </td>
                </tr>
              );
            })}
            {!loading && courses.length === 0 && (
              <tr><td colSpan={6}>
                <EmptyState
                  icon={<GraduationCap className="w-10 h-10" />}
                  title="No courses"
                  description={typeFilter !== "all"
                    ? `No ${typeFilter} courses found for this period`
                    : "No courses match your filters"}
                />
              </td></tr>
            )}
          </tbody>
        </table>

        {!loading && courses.length > 0 && (
          <div className="px-4 py-2.5 bg-gray-50 border-t border-gray-200 flex items-center justify-between text-xs text-gray-500">
            <span>{courses.length} course{courses.length !== 1 ? "s" : ""}</span>
            <span>{totalAttendees} total attendees</span>
          </div>
        )}
      </div>
    </>
  );
}
