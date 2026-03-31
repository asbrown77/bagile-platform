"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  MonitoringCourse, RevenueSummary, PendingTransfer,
  getMonitoring, getRevenueSummary, getPendingTransfers, getDashboardOverview,
  formatCurrency, formatDate,
} from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonCard } from "@/components/ui/Skeleton";
import { statusBadge } from "@/components/ui/Badge";
import {
  GraduationCap, TrendingUp, AlertTriangle, ArrowLeftRight,
  Users, Calendar, ChevronRight, CheckCircle
} from "lucide-react";
import Link from "next/link";

export default function Dashboard() {
  const apiKey = useApiKey();
  const [courses, setCourses] = useState<MonitoringCourse[]>([]);
  const [revenue, setRevenue] = useState<RevenueSummary | null>(null);
  const [pendingTransfers, setPendingTransfers] = useState<PendingTransfer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!apiKey) return;
    loadData();
  }, [apiKey]);

  async function loadData() {
    try {
      // Single aggregated call — falls back to parallel if endpoint unavailable
      try {
        const overview = await getDashboardOverview(apiKey);
        setCourses((overview.monitoring || []).filter((c) =>
          c.title && c.title !== "N/A" && c.courseCode && c.courseCode !== ""
        ));
        setRevenue(overview.revenue);
        if (overview.pendingTransferCount > 0) {
          setPendingTransfers(await getPendingTransfers(apiKey));
        }
      } catch {
        const [monitoring, rev] = await Promise.all([
          getMonitoring(apiKey, 60),
          getRevenueSummary(apiKey),
        ]);
        setCourses(monitoring.filter((c) =>
          c.title && c.title !== "N/A" && c.courseCode && c.courseCode !== ""
        ));
        setRevenue(rev);
        try { setPendingTransfers(await getPendingTransfers(apiKey)); } catch {}
      }
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

  const todaysCourses = courses.filter((c) => getStatus(c) === "running");
  const atRiskCourses = courses.filter((c) => getStatus(c) === "at risk");
  const upcomingCourses = courses.filter((c) => {
    const s = getStatus(c);
    return s !== "running" && s !== "completed";
  });

  // Students trained = sum of attendees from monthly breakdown
  const studentsThisMonth = revenue?.monthlyBreakdown
    .find((m) => m.month === new Date().getMonth() + 1)?.attendeeCount ?? 0;
  const studentsThisYear = revenue?.monthlyBreakdown
    .reduce((sum, m) => sum + m.attendeeCount, 0) ?? 0;

  // Fair YTD comparison: same months only
  const previousYtd = revenue?.previousYearYtdRevenue ?? 0;
  const yoyChange = revenue && previousYtd > 0
    ? Math.round(((revenue.currentYearRevenue - previousYtd) / previousYtd) * 100)
    : null;

  const greeting = new Date().getHours() < 12 ? "Good morning" : new Date().getHours() < 18 ? "Good afternoon" : "Good evening";

  return (
    <>
      <PageHeader title="Dashboard" subtitle={`${greeting} — here's your overview`} />

      {error && <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>}

      {/* KPI Cards */}
      {loading ? (
        <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-6">
          {[1, 2, 3, 4, 5].map((i) => <SkeletonCard key={i} />)}
        </div>
      ) : (
        <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-6">
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
          <Card
            label="Students This Month"
            value={studentsThisMonth}
            subtitle="trained"
            icon={<Users className="w-4 h-4" />}
          />
          <Card
            label="Students YTD"
            value={studentsThisYear}
            subtitle="trained this year"
            icon={<Users className="w-4 h-4" />}
          />
          <Card
            label="Upcoming Courses"
            value={upcomingCourses.length}
            subtitle="next 60 days"
            icon={<Calendar className="w-4 h-4" />}
          />
        </div>
      )}

      {/* Alerts row */}
      <div className="flex flex-col gap-3 mb-6">
        {pendingTransfers.length > 0 && (
          <Link href="/transfers">
            <AlertBanner variant="warning">
              <strong>{pendingTransfers.length}</strong> attendee{pendingTransfers.length !== 1 ? "s" : ""} pending transfer — click to manage
            </AlertBanner>
          </Link>
        )}
      </div>

      {/* Today's Courses */}
      {!loading && todaysCourses.length > 0 && (
        <div className="mb-8">
          <div className="flex items-center gap-2 mb-3">
            <div className="w-2 h-2 rounded-full bg-blue-500 animate-pulse" />
            <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">Running Today</h2>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {todaysCourses.map((c) => (
              <Link key={c.id} href={`/courses/${c.id}`}
                className="bg-blue-50 border border-blue-200 rounded-xl p-4 hover:bg-blue-100 transition-colors">
                <div className="flex items-start justify-between">
                  <div>
                    <p className="font-semibold text-blue-900">{c.title}</p>
                    <p className="text-xs text-blue-600 font-mono mt-0.5">{c.courseCode}</p>
                  </div>
                  {statusBadge("running")}
                </div>
                <div className="flex items-center gap-4 mt-3 text-sm text-blue-700">
                  <span><Users className="w-3.5 h-3.5 inline mr-1" />{c.currentEnrolmentCount} attendees</span>
                  {c.trainerName && <span>{c.trainerName}</span>}
                </div>
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* At-Risk Courses */}
      {!loading && atRiskCourses.length > 0 && (
        <div className="mb-8">
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-2">
              <AlertTriangle className="w-4 h-4 text-red-500" />
              <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">At Risk</h2>
              <span className="bg-red-100 text-red-700 text-xs font-bold px-2 py-0.5 rounded-full">{atRiskCourses.length}</span>
            </div>
            <Link href="/courses" className="text-xs text-brand-600 hover:text-brand-700 flex items-center gap-1">
              View all courses <ChevronRight className="w-3 h-3" />
            </Link>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {atRiskCourses.slice(0, 6).map((c) => (
              <Link key={c.id} href={`/courses/${c.id}`}
                className="bg-red-50 border border-red-200 rounded-xl p-4 hover:bg-red-100 transition-colors">
                <div className="flex items-start justify-between">
                  <div>
                    <p className="font-semibold text-red-900">{c.title}</p>
                    <p className="text-xs text-red-600 font-mono mt-0.5">{c.courseCode}</p>
                  </div>
                  {statusBadge("at risk")}
                </div>
                <div className="flex items-center gap-4 mt-3 text-sm text-red-700">
                  <span className="font-bold">{c.currentEnrolmentCount} enrolled</span>
                  <span>{formatDate(c.startDate)}</span>
                  <span>{c.daysUntilStart}d away</span>
                </div>
                <p className="text-xs text-red-500 mt-1">{c.recommendedAction}</p>
              </Link>
            ))}
          </div>
        </div>
      )}

      {/* All clear message if nothing at risk or running */}
      {!loading && todaysCourses.length === 0 && atRiskCourses.length === 0 && pendingTransfers.length === 0 && (
        <div className="bg-green-50 border border-green-200 rounded-xl p-6 mb-8 text-center">
          <CheckCircle className="w-8 h-8 text-green-500 mx-auto mb-2" />
          <p className="text-sm font-medium text-green-800">All clear — no courses running today, nothing at risk</p>
        </div>
      )}

      {/* Upcoming courses preview */}
      {!loading && upcomingCourses.length > 0 && (
        <div>
          <div className="flex items-center justify-between mb-3">
            <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">Upcoming</h2>
            <Link href="/courses" className="text-xs text-brand-600 hover:text-brand-700 flex items-center gap-1">
              View all <ChevronRight className="w-3 h-3" />
            </Link>
          </div>
          <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
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
                {upcomingCourses.slice(0, 10).map((c) => {
                  const status = getStatus(c);
                  return (
                    <tr key={c.id}
                      className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors"
                      onClick={() => window.location.href = `/courses/${c.id}`}>
                      <td className="px-4 py-3">
                        <p className="font-medium text-gray-900">{c.title}</p>
                        <p className="text-xs text-gray-400 font-mono">{c.courseCode}</p>
                      </td>
                      <td className="px-4 py-3 text-gray-600">
                        {formatDate(c.startDate)}
                        <p className="text-xs text-gray-400">{c.daysUntilStart}d away</p>
                      </td>
                      <td className="px-4 py-3 text-gray-600 hidden md:table-cell">{c.trainerName || "—"}</td>
                      <td className="px-4 py-3 text-center">
                        <span className={`text-lg font-bold ${status === "at risk" ? "text-red-600" : status === "guaranteed" ? "text-green-600" : "text-gray-700"}`}>
                          {c.currentEnrolmentCount}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-center">{statusBadge(status)}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </>
  );
}
