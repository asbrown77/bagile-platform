"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  MonitoringCourse, RevenueSummary, PendingTransfer,
  getMonitoring, getRevenueSummary, getPendingTransfers,
  formatCurrency, formatDate,
} from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonCard, SkeletonRow } from "@/components/ui/Skeleton";
import { statusBadge } from "@/components/ui/Badge";
import { GraduationCap, TrendingUp, AlertTriangle, ArrowLeftRight } from "lucide-react";
import Link from "next/link";

export default function Dashboard() {
  const apiKey = useApiKey();
  const [courses, setCourses] = useState<MonitoringCourse[]>([]);
  const [revenue, setRevenue] = useState<RevenueSummary | null>(null);
  const [pendingTransfers, setPendingTransfers] = useState<PendingTransfer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  // Filters
  const [trainerFilter, setTrainerFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [courseTypeFilter, setCourseTypeFilter] = useState("all");

  useEffect(() => {
    if (!apiKey) return;
    loadData();
  }, [apiKey]);

  async function loadData() {
    try {
      const [monitoring, rev] = await Promise.all([
        getMonitoring(apiKey, 60),
        getRevenueSummary(apiKey),
      ]);
      setCourses(monitoring);
      setRevenue(rev);
      try { setPendingTransfers(await getPendingTransfers(apiKey)); } catch {}
    } catch {
      setError("Failed to load dashboard data");
    } finally {
      setLoading(false);
    }
  }

  // Derived data
  const today = new Date(); today.setHours(0, 0, 0, 0);
  const getStatus = (c: MonitoringCourse) => {
    const start = new Date(c.startDate); start.setHours(0, 0, 0, 0);
    const end = c.endDate ? new Date(c.endDate) : start; end.setHours(0, 0, 0, 0);
    if (start <= today && today <= end) return "running";
    if (today > end) return "completed";
    if (c.currentEnrolmentCount < 3 && c.daysUntilStart <= 7) return "at risk";
    return c.currentEnrolmentCount >= 3 ? "guaranteed" : "monitor";
  };

  const trainers = [...new Set(courses.map((c) => c.trainerName).filter(Boolean))] as string[];
  const courseTypes = [...new Set(courses.map((c) => c.courseCode?.split("-")[0]).filter(Boolean))].sort() as string[];

  const filtered = courses.filter((c) => {
    if (trainerFilter !== "all" && c.trainerName !== trainerFilter) return false;
    if (courseTypeFilter !== "all" && !c.courseCode?.startsWith(courseTypeFilter)) return false;
    if (statusFilter !== "all" && getStatus(c) !== statusFilter) return false;
    return true;
  });

  const atRiskCount = courses.filter((c) => getStatus(c) === "at risk").length;
  const yoyChange = revenue && revenue.previousYearRevenue > 0
    ? Math.round(((revenue.currentYearRevenue - revenue.previousYearRevenue) / revenue.previousYearRevenue) * 100)
    : null;

  return (
    <>
      <PageHeader title="Dashboard" subtitle="Good morning — here's your overview" />

      {error && <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>}

      {/* KPI Cards */}
      {loading ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          {[1, 2, 3, 4].map((i) => <SkeletonCard key={i} />)}
        </div>
      ) : (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          <Card
            label="Upcoming Courses"
            value={courses.length}
            subtitle="next 60 days"
            icon={<GraduationCap className="w-4 h-4" />}
          />
          <Card
            label="At Risk"
            value={atRiskCount}
            variant={atRiskCount > 0 ? "danger" : "success"}
            subtitle={atRiskCount > 0 ? "need action" : "all on track"}
            icon={<AlertTriangle className="w-4 h-4" />}
          />
          <Card
            label="Revenue This Month"
            value={revenue ? formatCurrency(revenue.currentMonthRevenue) : "—"}
            subtitle={`${revenue?.currentMonthOrders ?? 0} orders`}
            icon={<TrendingUp className="w-4 h-4" />}
          />
          <Card
            label="Revenue YTD"
            value={revenue ? formatCurrency(revenue.currentYearRevenue) : "—"}
            trend={yoyChange !== null ? { value: yoyChange, isPositive: yoyChange >= 0 } : undefined}
            subtitle="vs previous year"
            icon={<TrendingUp className="w-4 h-4" />}
          />
        </div>
      )}

      {/* Pending transfers alert */}
      {pendingTransfers.length > 0 && (
        <div className="mb-6">
          <Link href="/transfers">
            <AlertBanner variant="warning">
              <strong>{pendingTransfers.length}</strong> attendee{pendingTransfers.length !== 1 ? "s" : ""} pending transfer — click to manage
            </AlertBanner>
          </Link>
        </div>
      )}

      {/* Filters */}
      {!loading && courses.length > 0 && (
        <div className="flex gap-3 mb-4 items-center flex-wrap">
          {trainers.length > 1 && (
            <select value={trainerFilter} onChange={(e) => setTrainerFilter(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white text-gray-700 focus:ring-2 focus:ring-brand-500 focus:border-brand-500">
              <option value="all">All trainers</option>
              {trainers.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          )}
          {courseTypes.length > 1 && (
            <select value={courseTypeFilter} onChange={(e) => setCourseTypeFilter(e.target.value)}
              className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white text-gray-700 focus:ring-2 focus:ring-brand-500 focus:border-brand-500">
              <option value="all">All courses</option>
              {courseTypes.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          )}
          <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white text-gray-700 focus:ring-2 focus:ring-brand-500 focus:border-brand-500">
            <option value="all">All statuses</option>
            <option value="running">Running</option>
            <option value="guaranteed">Guaranteed</option>
            <option value="monitor">Monitor</option>
            <option value="at risk">At Risk</option>
            <option value="completed">Completed</option>
          </select>
          <span className="text-xs text-gray-400">{filtered.length} course{filtered.length !== 1 ? "s" : ""}</span>
        </div>
      )}

      {/* Course table */}
      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Course</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Date</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Trainer</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Enrolled</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden lg:table-cell">Action</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={6} />)}

            {!loading && filtered.map((c) => {
              const status = getStatus(c);
              return (
                <tr
                  key={c.id}
                  className={`border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors ${status === "at risk" ? "bg-red-50/40" : ""}`}
                  onClick={() => window.location.href = `/courses/${c.id}`}
                >
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
                  <td className="px-4 py-3 text-xs text-gray-500 hidden lg:table-cell">{c.recommendedAction}</td>
                </tr>
              );
            })}

            {!loading && filtered.length === 0 && (
              <tr>
                <td colSpan={6}>
                  <EmptyState
                    icon={<GraduationCap className="w-10 h-10" />}
                    title="No courses match"
                    description="Try adjusting your filters or check back later"
                  />
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
