"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { MonitoringCourse, getMonitoring, formatDate } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { Badge, statusBadge } from "@/components/ui/Badge";
import { GraduationCap } from "lucide-react";

export default function CoursesPage() {
  const apiKey = useApiKey();
  const [courses, setCourses] = useState<MonitoringCourse[]>([]);
  const [loading, setLoading] = useState(true);
  const [statusFilter, setStatusFilter] = useState("all");
  const [typeFilter, setTypeFilter] = useState("all");

  useEffect(() => {
    if (!apiKey) return;
    getMonitoring(apiKey, 90)
      .then((data) => setCourses(data.filter((c) => c.title && c.title !== "N/A" && c.courseCode)))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey]);

  const today = new Date(); today.setHours(0, 0, 0, 0);
  const getStatus = (c: MonitoringCourse) => {
    const start = new Date(c.startDate); start.setHours(0, 0, 0, 0);
    const end = c.endDate ? new Date(c.endDate) : start; end.setHours(0, 0, 0, 0);
    if (start <= today && today <= end) return "running";
    if (today > end) return "completed";
    if (c.currentEnrolmentCount < 3 && c.daysUntilStart <= 7) return "at risk";
    return c.currentEnrolmentCount >= 3 ? "guaranteed" : "monitor";
  };

  const courseTypes = [...new Set(courses.map((c) => c.courseCode?.split("-")[0]).filter(Boolean))].sort() as string[];

  const filtered = courses.filter((c) => {
    const status = getStatus(c);
    if (statusFilter !== "all" && status !== statusFilter) return false;
    if (typeFilter !== "all" && !c.courseCode?.startsWith(typeFilter)) return false;
    return true;
  });

  const isVirtual = (c: MonitoringCourse) => c.location?.toLowerCase().includes("virtual");

  return (
    <>
      <PageHeader title="Courses" subtitle="All upcoming and recent courses" />

      {/* Filters */}
      <div className="flex gap-3 mb-4 items-center flex-wrap">
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}
          className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
          <option value="all">All statuses</option>
          <option value="running">Running</option>
          <option value="guaranteed">Guaranteed</option>
          <option value="monitor">Monitor</option>
          <option value="at risk">At Risk</option>
          <option value="completed">Completed</option>
        </select>
        {courseTypes.length > 1 && (
          <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
            <option value="all">All types</option>
            {courseTypes.map((t) => <option key={t} value={t}>{t}</option>)}
          </select>
        )}
        <span className="text-xs text-gray-400">{filtered.length} course{filtered.length !== 1 ? "s" : ""}</span>
      </div>

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
            {!loading && filtered.map((c) => {
              const status = getStatus(c);
              return (
                <tr key={c.id}
                  className={`border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors ${status === "at risk" ? "bg-red-50/40" : ""}`}
                  onClick={() => window.location.href = `/courses/${c.id}`}>
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-900">{c.title}</p>
                    <p className="text-xs text-gray-400 font-mono">{c.courseCode}</p>
                  </td>
                  <td className="px-4 py-3 text-gray-600">
                    {formatDate(c.startDate)}
                    <p className="text-xs text-gray-400">
                      {c.daysUntilStart > 0 ? `${c.daysUntilStart}d away` : c.daysUntilStart === 0 ? "Today" : "Past"}
                    </p>
                  </td>
                  <td className="px-4 py-3 text-gray-600 hidden md:table-cell">{c.trainerName || "—"}</td>
                  <td className="px-4 py-3 text-center">
                    <span className={`text-lg font-bold ${status === "at risk" ? "text-red-600" : status === "guaranteed" || status === "running" ? "text-green-600" : "text-gray-700"}`}>
                      {c.currentEnrolmentCount}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center">{statusBadge(status)}</td>
                  <td className="px-4 py-3 text-center hidden lg:table-cell">
                    <Badge variant={isVirtual(c) ? "info" : "neutral"} dot>
                      {isVirtual(c) ? "Virtual" : "In-person"}
                    </Badge>
                  </td>
                </tr>
              );
            })}
            {!loading && filtered.length === 0 && (
              <tr><td colSpan={6}>
                <EmptyState icon={<GraduationCap className="w-10 h-10" />} title="No courses" description="No courses match your filters" />
              </td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
