"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { MonitoringCourse, getMonitoring, formatDate } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { statusBadge } from "@/components/ui/Badge";
import { GraduationCap } from "lucide-react";

export default function CoursesPage() {
  const apiKey = useApiKey();
  const [courses, setCourses] = useState<MonitoringCourse[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!apiKey) return;
    getMonitoring(apiKey, 90)
      .then(setCourses)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey]);

  return (
    <>
      <PageHeader title="Courses" subtitle="All upcoming and recent courses" />
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Course</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Date</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Trainer</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Enrolled</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={5} />)}
            {!loading && courses.map((c) => {
              const today = new Date(); today.setHours(0, 0, 0, 0);
              const start = new Date(c.startDate); start.setHours(0, 0, 0, 0);
              const end = c.endDate ? new Date(c.endDate) : start; end.setHours(0, 0, 0, 0);
              const status = start <= today && today <= end ? "running"
                : today > end ? "completed"
                : c.currentEnrolmentCount < 3 && c.daysUntilStart <= 7 ? "at risk"
                : c.currentEnrolmentCount >= 3 ? "guaranteed" : "monitor";
              return (
                <tr key={c.id} className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer"
                  onClick={() => window.location.href = `/courses/${c.id}`}>
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-900">{c.title}</p>
                    <p className="text-xs text-gray-400 font-mono">{c.courseCode}</p>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{formatDate(c.startDate)}</td>
                  <td className="px-4 py-3 text-gray-600 hidden md:table-cell">{c.trainerName || "—"}</td>
                  <td className="px-4 py-3 text-center font-bold text-gray-700">{c.currentEnrolmentCount}</td>
                  <td className="px-4 py-3 text-center">{statusBadge(status)}</td>
                </tr>
              );
            })}
            {!loading && courses.length === 0 && (
              <tr><td colSpan={5}>
                <EmptyState icon={<GraduationCap className="w-10 h-10" />} title="No courses" description="No upcoming courses found" />
              </td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
