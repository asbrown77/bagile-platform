"use client";

import { useEffect, useState, useCallback } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  MonitoringCourse, RevenueSummary, PendingTransfer, CourseScheduleItem,
  getMonitoring, getRevenueSummary, getPendingTransfers, getDashboardOverview,
  getCourseSchedules, formatCurrency, formatDate,
} from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { SkeletonCard } from "@/components/ui/Skeleton";
import { statusBadge } from "@/components/ui/Badge";
import {
  TrendingUp, AlertTriangle,
  Users, Calendar, ChevronRight, CheckCircle, ChevronLeft,
} from "lucide-react";
import Link from "next/link";
import { getCourseDisplayStatus } from "@/lib/courseStatus";
import {
  getCourseColour, getTrainerColour, trainerInitials,
} from "@/lib/courseColours";
import { loadConfig } from "@/lib/config";

// ── Calendar helpers (duplicated from calendar page to avoid coupling) ───────

const DAY_NAMES_SHORT = ["Mon", "Tue", "Wed", "Thu", "Fri"];
const MONTH_NAMES = ["Jan", "Feb", "Mar", "Apr", "May", "Jun",
  "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

/** Format a Date as YYYY-MM-DD using local time (not UTC) to avoid timezone
 *  off-by-one errors in BST/CET. toISOString() would give the UTC date which
 *  can be a day behind for dates at midnight in UTC+1 or later. */
function toDateStr(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, "0");
  const day = String(d.getDate()).padStart(2, "0");
  return `${y}-${m}-${day}`;
}

function addDays(d: Date, n: number): Date {
  const r = new Date(d);
  r.setDate(r.getDate() + n);
  return r;
}

/** Returns Monday of the ISO week containing d. */
function getWeekStart(d: Date): Date {
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  const monday = new Date(d);
  monday.setDate(d.getDate() + diff);
  monday.setHours(0, 0, 0, 0);
  return monday;
}

// ── Week strip compact pill component ───────────────────────────────────────

interface WeekPillProps {
  course: CourseScheduleItem;
  minEnrolments: number;
  cellDate: string;
  showCancelled: boolean;
}

function WeekPill({ course, minEnrolments, cellDate, showCancelled }: WeekPillProps) {
  const status = getCourseDisplayStatus(course);
  if (status === "cancelled" && !showCancelled) return null;

  const isCancelled = status === "cancelled";
  const isAtRisk = status === "at risk" || status === "cancel";
  const isPrivate = course.type === "private";
  const firstSegment = course.courseCode?.split("-")[0] || "";
  const borderColour = getCourseColour(course.courseCode || "");
  const initials = trainerInitials(course.trainerName);
  const avatarColour = getTrainerColour(initials);

  // Multi-day
  const startStr = (course.startDate || "").split("T")[0];
  const endStr = (course.endDate || course.startDate || "").split("T")[0];
  const spanDays = startStr && endStr
    ? Math.round((new Date(endStr).getTime() - new Date(startStr).getTime()) / 86400000) + 1
    : 1;
  const isMultiDay = spanDays > 1;
  const dayIndex = cellDate && startStr
    ? Math.round((new Date(cellDate).getTime() - new Date(startStr).getTime()) / 86400000) + 1
    : 1;
  const isDay1 = dayIndex === 1;

  const enrolDisplay = `${course.currentEnrolmentCount}/${minEnrolments}`;

  return (
    <Link
      href={`/courses/${course.id}`}
      title={`${course.title} — ${course.currentEnrolmentCount} enrolled${isPrivate ? " (private)" : ""}${isCancelled ? " — CANCELLED" : ""}`}
      className={`block rounded border text-[10px] font-medium transition-opacity hover:opacity-80
        bg-white
        ${isCancelled ? "opacity-50" : ""}
        ${isMultiDay && !isDay1 ? "border-dashed border-gray-200" : "border-gray-200"}`}
      style={{
        borderLeftWidth: 3,
        borderLeftColor: borderColour,
        borderLeftStyle: isMultiDay && !isDay1 ? "dashed" : "solid",
      }}
    >
      <div className="px-1 py-0.5 min-w-0">
        <div className="flex items-center gap-0.5 min-w-0">
          <span className={`font-semibold truncate text-gray-800 ${isCancelled ? "line-through" : ""}`}>
            {isMultiDay ? `${firstSegment} (${dayIndex}/${spanDays})` : firstSegment}
          </span>
          {isPrivate && (
            <span className="shrink-0 text-[8px] bg-amber-100 text-amber-700 rounded px-0.5 leading-tight font-bold">P</span>
          )}
        </div>
        <div className="flex items-center gap-0.5 mt-0.5">
          {course.trainerName && (
            <span
              className="inline-flex items-center justify-center rounded-full text-white font-bold leading-none shrink-0"
              style={{ backgroundColor: avatarColour, width: 12, height: 12, fontSize: 7 }}
            >
              {initials}
            </span>
          )}
          {isAtRisk && <span className="text-amber-500 shrink-0" style={{ fontSize: 8 }}>▲</span>}
          <span className="text-gray-400 shrink-0">{enrolDisplay}</span>
        </div>
      </div>
    </Link>
  );
}

// ── Week strip ───────────────────────────────────────────────────────────────

interface WeekStripProps {
  apiKey: string;
  showCancelled: boolean;
}

function WeekStrip({ apiKey, showCancelled }: WeekStripProps) {
  const now = new Date();
  const [weekStart, setWeekStart] = useState(() => getWeekStart(now));
  const [courses, setCourses] = useState<CourseScheduleItem[]>([]);
  const [loading, setLoading] = useState(true);
  const { minEnrolments } = loadConfig();

  const today = new Date(); today.setHours(0, 0, 0, 0);
  const todayStr = toDateStr(today);

  // Mon-Fri only
  const weekDays = Array.from({ length: 5 }, (_, i) => addDays(weekStart, i));

  const weekEndDate = addDays(weekStart, 4); // Fri
  const weekLabel = weekStart.getMonth() === weekEndDate.getMonth()
    ? `${MONTH_NAMES[weekStart.getMonth()]} ${weekStart.getDate()}–${weekEndDate.getDate()}`
    : `${MONTH_NAMES[weekStart.getMonth()]} ${weekStart.getDate()} – ${MONTH_NAMES[weekEndDate.getMonth()]} ${weekEndDate.getDate()}`;

  const loadWeekCourses = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);
    try {
      // Fetch Mon–Fri + 1 day buffer each side to catch multi-day courses
      const from = toDateStr(addDays(weekStart, -1));
      const to = toDateStr(addDays(weekStart, 6));
      const result = await getCourseSchedules(apiKey, { from, to, pageSize: 50 });
      setCourses((result.items || []).filter((c) => c.title && c.startDate));
    } catch {
      // Non-critical — week strip failing shouldn't break the page
    } finally {
      setLoading(false);
    }
  }, [apiKey, weekStart]);

  useEffect(() => { loadWeekCourses(); }, [loadWeekCourses]);

  function coursesOnDay(dateStr: string): CourseScheduleItem[] {
    return courses.filter((c) => {
      const start = (c.startDate || "").split("T")[0];
      const end = (c.endDate || c.startDate || "").split("T")[0];
      return dateStr >= start && dateStr <= end;
    });
  }

  return (
    <div className="mb-8">
      {/* Strip header */}
      <div className="flex items-center justify-between mb-2">
        <div className="flex items-center gap-2">
          <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">This Week</h2>
          <span className="text-xs text-gray-400">{weekLabel}</span>
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={() => setWeekStart((ws) => addDays(ws, -7))}
            className="p-1 rounded hover:bg-gray-100"
            title="Previous week"
          >
            <ChevronLeft className="w-4 h-4 text-gray-500" />
          </button>
          <button
            onClick={() => setWeekStart(getWeekStart(new Date()))}
            className="text-xs text-brand-600 hover:text-brand-700 font-medium px-1"
          >
            Today
          </button>
          <button
            onClick={() => setWeekStart((ws) => addDays(ws, 7))}
            className="p-1 rounded hover:bg-gray-100"
            title="Next week"
          >
            <ChevronRight className="w-4 h-4 text-gray-500" />
          </button>
          <Link href="/courses/calendar" className="text-xs text-brand-600 hover:text-brand-700 ml-2">
            Full calendar
          </Link>
        </div>
      </div>

      {/* 5-column strip */}
      <div className="grid grid-cols-5 gap-1 bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        {weekDays.map((d, i) => {
          const ds = toDateStr(d);
          const isToday = ds === todayStr;
          const dayCourses = coursesOnDay(ds);

          return (
            <div
              key={i}
              className={`min-h-[80px] flex flex-col border-r border-gray-100 last:border-r-0
                ${isToday ? "bg-blue-50/60" : ""}`}
            >
              {/* Day header */}
              <div className={`px-2 py-1.5 border-b border-gray-100 text-center
                ${isToday ? "bg-blue-100/60 border-blue-200" : "bg-gray-50"}`}>
                <p className="text-[10px] font-medium text-gray-500 uppercase">{DAY_NAMES_SHORT[i]}</p>
                <p className={`text-sm font-semibold
                  ${isToday ? "text-blue-700" : "text-gray-700"}`}>
                  {d.getDate()}
                </p>
              </div>

              {/* Course pills */}
              <div className="flex-1 p-1 space-y-0.5">
                {loading && i === 0 && (
                  <div className="text-[9px] text-gray-400 p-1">Loading…</div>
                )}
                {dayCourses.map((c) => (
                  <WeekPill
                    key={`${c.id}-${ds}`}
                    course={c}
                    minEnrolments={minEnrolments}
                    cellDate={ds}
                    showCancelled={showCancelled}
                  />
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ── Dashboard ────────────────────────────────────────────────────────────────

export default function Dashboard() {
  const apiKey = useApiKey();
  const [courses, setCourses] = useState<MonitoringCourse[]>([]);
  const [revenue, setRevenue] = useState<RevenueSummary | null>(null);
  const [pendingTransfers, setPendingTransfers] = useState<PendingTransfer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showCancelled, setShowCancelled] = useState(false);

  useEffect(() => {
    if (apiKey === null) return;          // still reading localStorage
    if (apiKey === "") {                  // key missing — send to login
      window.location.replace("/login");
      return;
    }
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
  const getStatus = (c: MonitoringCourse) => getCourseDisplayStatus(c);

  const todaysCourses = courses.filter((c) => getStatus(c) === "running");
  const atRiskCourses = courses.filter((c) => getStatus(c) === "at risk");
  const upcomingCourses = courses.filter((c) => {
    const s = getStatus(c);
    return s !== "running" && s !== "completed" && s !== "cancelled";
  });

  const studentsThisMonth = revenue?.monthlyBreakdown
    .find((m) => m.month === new Date().getMonth() + 1)?.attendeeCount ?? 0;
  const studentsThisYear = revenue?.monthlyBreakdown
    .reduce((sum, m) => sum + m.attendeeCount, 0) ?? 0;

  const previousYtd = revenue?.previousYearYtdRevenue ?? 0;
  const yoyChange = revenue && previousYtd > 0
    ? Math.round(((revenue.currentYearRevenue - previousYtd) / previousYtd) * 100)
    : null;

  const hour = new Date().getHours();
  const greeting = hour < 12 ? "Good morning" : hour < 18 ? "Good afternoon" : "Good evening";

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
            {atRiskCourses.slice(0, 6).map((c) => {
              const daysAway = c.daysUntilStart != null
                ? c.daysUntilStart
                : Math.round((new Date(c.startDate).setHours(0, 0, 0, 0) - new Date().setHours(0, 0, 0, 0)) / 86400000);
              return (
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
                  <span>({daysAway}d away)</span>
                </div>
                <p className="text-xs text-red-500 mt-1">{c.recommendedAction}</p>
              </Link>
              );
            })}
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

      {/* Week strip — loads independently so it doesn't block the dashboard */}
      {apiKey && (
        <WeekStrip apiKey={apiKey} showCancelled={showCancelled} />
      )}

      {/* Upcoming courses table */}
      {!loading && upcomingCourses.length > 0 && (
        <div>
          <div className="flex items-center justify-between mb-3">
            <div className="flex items-center gap-3">
              <h2 className="text-sm font-semibold text-gray-900 uppercase tracking-wide">Upcoming</h2>
              {/* Cancelled toggle — shared with week strip */}
              <label className="flex items-center gap-1.5 text-xs text-gray-500 cursor-pointer select-none font-normal">
                <input
                  type="checkbox"
                  checked={showCancelled}
                  onChange={(e) => setShowCancelled(e.target.checked)}
                  className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
                />
                Show cancelled
              </label>
            </div>
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
                  const daysAway = c.daysUntilStart != null
                    ? c.daysUntilStart
                    : Math.round((new Date(c.startDate).setHours(0, 0, 0, 0) - new Date().setHours(0, 0, 0, 0)) / 86400000);
                  return (
                    <tr key={c.id}
                      className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors"
                      onClick={() => window.location.href = `/courses/${c.id}`}>
                      <td className="px-4 py-3">
                        <p className="font-medium text-gray-900">{c.title}</p>
                        <p className="text-xs text-gray-400 font-mono">{c.courseCode}</p>
                      </td>
                      <td className="px-4 py-3 text-gray-600">
                        <p>{formatDate(c.startDate)}</p>
                        <p className="text-xs text-gray-400">({daysAway}d away)</p>
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
