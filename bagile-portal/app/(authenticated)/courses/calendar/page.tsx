"use client";

import { useEffect, useState, useCallback } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { CourseScheduleItem, getCourseSchedules } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { Button } from "@/components/ui/Button";
import { ChevronLeft, ChevronRight, List, CalendarDays, CalendarRange } from "lucide-react";
import Link from "next/link";
import { getCourseDisplayStatus } from "@/lib/courseStatus";
import { loadConfig } from "@/lib/config";
import {
  getCourseColour, getTrainerColour, trainerInitials,
} from "@/lib/courseColours";

const DAY_NAMES = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
const MONTH_NAMES = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"];

type ViewMode = "week" | "month";

/** Background tint applied to the whole tile body, keyed by display status. */
const STATUS_BG: Record<string, string> = {
  running:   "bg-blue-50 border-gray-200",
  completed: "bg-gray-50 border-gray-200",
  cancel:    "bg-red-50 border-red-200",
  "at risk": "bg-red-50 border-red-200",
  guaranteed:"bg-green-50 border-gray-200",
  monitor:   "bg-amber-50 border-gray-200",
  cancelled: "bg-gray-50 border-gray-100 opacity-50",
};

function getViewMode(): ViewMode {
  if (typeof window === "undefined") return "month";
  return (localStorage.getItem("bagile_cal_view") as ViewMode) || "month";
}

function saveViewMode(v: ViewMode) {
  localStorage.setItem("bagile_cal_view", v);
}

/** Returns Monday of the ISO week containing the given date. */
function getWeekStart(d: Date): Date {
  const day = d.getDay(); // 0=Sun
  const diff = (day === 0 ? -6 : 1 - day);
  const monday = new Date(d);
  monday.setDate(d.getDate() + diff);
  monday.setHours(0, 0, 0, 0);
  return monday;
}

function toDateStr(d: Date): string {
  return d.toISOString().split("T")[0];
}

function addDays(d: Date, n: number): Date {
  const r = new Date(d);
  r.setDate(r.getDate() + n);
  return r;
}

interface CourseTileProps {
  course: CourseScheduleItem;
  minEnrolments: number;
  compact?: boolean;
  /** ISO date string for the cell this tile is rendered in (for multi-day day index). */
  cellDate?: string;
}

function CourseTile({ course, minEnrolments, compact = false, cellDate }: CourseTileProps) {
  const status = getCourseDisplayStatus(course);
  const bgClass = STATUS_BG[status] || STATUS_BG.monitor;
  const isCancelled = status === "cancelled";
  const isAtRisk = status === "at risk" || status === "cancel";
  const isPrivate = course.type === "private";

  // Course type prefix for display (e.g. "PSMAI" → "PSM-AI" is kept as-is from API)
  const firstSegment = course.courseCode?.split("-")[0] || "";
  const borderColour = getCourseColour(course.courseCode || "");

  // Trainer avatar
  const initials = trainerInitials(course.trainerName);
  const avatarColour = getTrainerColour(initials);

  // Multi-day: determine day index (1-based) and total span
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

  // Enrolment display
  const enrolDisplay = `${course.currentEnrolmentCount}/${minEnrolments}`;

  // Date display for non-compact tiles
  const dateLabel = course.startDate
    ? new Date(course.startDate).toLocaleDateString("en-GB", { day: "numeric", month: "short" })
    : "";

  return (
    <Link
      href={`/courses/${course.id}`}
      title={`${course.title} — ${course.currentEnrolmentCount} enrolled${isPrivate ? " (private)" : ""}${isCancelled ? " — CANCELLED" : ""}`}
      className={`block rounded border text-[10px] md:text-xs font-medium transition-opacity hover:opacity-80
        ${bgClass}
        ${isCancelled ? "opacity-50" : ""}
        ${isMultiDay && !isDay1 ? "border-dashed" : "border-solid"}`}
      style={{ borderLeftWidth: 3, borderLeftColor: borderColour, borderLeftStyle: isMultiDay && !isDay1 ? "dashed" : "solid" }}
    >
      <div className="px-1.5 py-1 min-w-0">
        {/* Top row: course code + private badge */}
        <div className="flex items-center gap-1 min-w-0">
          <span className={`font-semibold truncate text-gray-800 ${isCancelled ? "line-through" : ""}`}>
            {isMultiDay
              ? `${firstSegment} (${dayIndex}/${spanDays})`
              : firstSegment}
          </span>
          {isPrivate && (
            <span className="shrink-0 text-[9px] bg-amber-100 text-amber-700 rounded px-1 py-0 leading-tight font-bold">P</span>
          )}
        </div>

        {/* Bottom row: trainer avatar · format · enrolment · date (hidden in compact) */}
        <div className="flex items-center gap-1 mt-0.5 flex-wrap">
          {/* Trainer avatar */}
          {course.trainerName && (
            <span
              className="inline-flex items-center justify-center rounded-full text-white font-bold leading-none shrink-0"
              style={{ backgroundColor: avatarColour, width: 14, height: 14, fontSize: 8 }}
            >
              {initials}
            </span>
          )}

          {/* Format indicator: V = virtual, F = face-to-face */}
          {course.formatType && (
            <span
              className={`shrink-0 text-[9px] font-bold rounded px-0.5 leading-tight ${
                course.formatType.toLowerCase().includes("virtual")
                  ? "bg-blue-100 text-blue-600"
                  : "bg-gray-100 text-gray-600"
              }`}
              title={course.formatType}
            >
              {course.formatType.toLowerCase().includes("virtual") ? "V" : "F2F"}
            </span>
          )}

          {/* At-risk indicator */}
          {isAtRisk && (
            <span className="shrink-0 text-amber-500" title="At risk / cancel">▲</span>
          )}

          {/* Enrolment */}
          {!compact && (
            <span className="text-gray-500 shrink-0">{enrolDisplay}</span>
          )}
          {compact && (
            <span className="text-gray-500 shrink-0">{course.currentEnrolmentCount}</span>
          )}

          {/* Date — only in full (non-compact) mode */}
          {!compact && dateLabel && (
            <span className="text-gray-400 shrink-0 hidden sm:inline">{dateLabel}</span>
          )}
        </div>
      </div>
    </Link>
  );
}

export default function CalendarPage() {
  const apiKey = useApiKey();
  const [courses, setCourses] = useState<CourseScheduleItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [viewMode, setViewMode] = useState<ViewMode>("month"); // initialised from localStorage after mount
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth()); // 0-indexed
  const [weekStart, setWeekStart] = useState(() => getWeekStart(now));
  /** Trainer filter: "AB" | "CB" | "all" */
  const [trainerFilter, setTrainerFilter] = useState<"AB" | "CB" | "all">("all");
  /** Whether to show cancelled courses (default: hidden). */
  const [showCancelled, setShowCancelled] = useState(false);
  const { minEnrolments } = loadConfig();

  // Restore persisted view mode after mount (avoids SSR mismatch)
  useEffect(() => {
    setViewMode(getViewMode());
  }, []);

  const loadCourses = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);
    setError("");

    let from: string;
    let to: string;
    if (viewMode === "week") {
      from = toDateStr(addDays(weekStart, -1));
      to = toDateStr(addDays(weekStart, 8));
    } else {
      from = new Date(year, month - 1, 1).toISOString().split("T")[0];
      to = new Date(year, month + 2, 0).toISOString().split("T")[0];
    }

    try {
      const result = await getCourseSchedules(apiKey, { from, to, pageSize: 100 });
      setCourses((result.items || []).filter((c) => c.title && c.startDate));
    } catch {
      setError("Failed to load courses");
    } finally {
      setLoading(false);
    }
  }, [apiKey, viewMode, year, month, weekStart]);

  useEffect(() => { loadCourses(); }, [loadCourses]);

  function switchView(v: ViewMode) {
    setViewMode(v);
    saveViewMode(v);
  }

  function prevMonth() {
    if (month === 0) { setMonth(11); setYear(year - 1); }
    else setMonth(month - 1);
  }
  function nextMonth() {
    if (month === 11) { setMonth(0); setYear(year + 1); }
    else setMonth(month + 1);
  }
  function prevWeek() { setWeekStart((ws) => addDays(ws, -7)); }
  function nextWeek() { setWeekStart((ws) => addDays(ws, 7)); }

  function goToday() {
    const t = new Date();
    setYear(t.getFullYear());
    setMonth(t.getMonth());
    setWeekStart(getWeekStart(t));
  }

  const today = new Date(); today.setHours(0, 0, 0, 0);
  const todayStr = toDateStr(today);

  function coursesOnDate(dateStr: string) {
    return courses.filter((c) => {
      const start = (c.startDate || "").split("T")[0];
      const end = (c.endDate || c.startDate || "").split("T")[0];
      if (dateStr < start || dateStr > end) return false;

      // Cancelled filter — hide unless toggled on
      if (!showCancelled && c.status === "cancelled") return false;

      // Trainer filter
      if (trainerFilter !== "all") {
        const initials = trainerInitials(c.trainerName);
        if (initials !== trainerFilter) return false;
      }

      return true;
    });
  }

  // ── Month view grid ─────────────────────────────────────────────────────────
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const startDow = (firstDay.getDay() + 6) % 7; // Monday=0
  const daysInMonth = lastDay.getDate();

  const weeks: (number | null)[][] = [];
  let currentWeek: (number | null)[] = [];
  for (let i = 0; i < startDow; i++) currentWeek.push(null);
  for (let d = 1; d <= daysInMonth; d++) {
    currentWeek.push(d);
    if (currentWeek.length === 7) { weeks.push(currentWeek); currentWeek = []; }
  }
  if (currentWeek.length > 0) {
    while (currentWeek.length < 7) currentWeek.push(null);
    weeks.push(currentWeek);
  }

  // ── Week view days ───────────────────────────────────────────────────────────
  const weekDays = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));
  const weekEndDate = addDays(weekStart, 6);
  const weekLabel = weekStart.getMonth() === weekEndDate.getMonth()
    ? `${MONTH_NAMES[weekStart.getMonth()]} ${weekStart.getDate()}–${weekEndDate.getDate()}, ${weekStart.getFullYear()}`
    : `${MONTH_NAMES[weekStart.getMonth()]} ${weekStart.getDate()} – ${MONTH_NAMES[weekEndDate.getMonth()]} ${weekEndDate.getDate()}, ${weekStart.getFullYear()}`;

  return (
    <>
      <PageHeader
        title="Course Calendar"
        actions={
          <Link href="/courses">
            <Button variant="secondary" size="sm"><List className="w-3.5 h-3.5" /> List View</Button>
          </Link>
        }
      />

      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

      {/* Toolbar: view toggle + filters + navigation */}
      <div className="flex items-center justify-between mb-4 gap-2 flex-wrap">
        {/* Left group: view toggle + trainer filter + cancelled toggle */}
        <div className="flex items-center gap-2 flex-wrap">
          {/* View mode toggle */}
          <div className="flex rounded-lg border border-gray-200 overflow-hidden">
            <button
              onClick={() => switchView("week")}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium transition-colors
                ${viewMode === "week" ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
            >
              <CalendarDays className="w-3.5 h-3.5" /> Week
            </button>
            <button
              onClick={() => switchView("month")}
              className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium border-l border-gray-200 transition-colors
                ${viewMode === "month" ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
            >
              <CalendarRange className="w-3.5 h-3.5" /> Month
            </button>
          </div>

          {/* Trainer filter */}
          <div className="flex rounded-lg border border-gray-200 overflow-hidden">
            {(["all", "AB", "CB"] as const).map((t, i) => (
              <button
                key={t}
                onClick={() => setTrainerFilter(t)}
                className={`px-3 py-1.5 text-xs font-medium transition-colors
                  ${trainerFilter === t ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}
                  ${i > 0 ? "border-l border-gray-200" : ""}`}
              >
                {t === "all" ? "Both" : t}
              </button>
            ))}
          </div>

          {/* Cancelled toggle */}
          <label className="flex items-center gap-1.5 text-xs text-gray-600 cursor-pointer select-none">
            <input
              type="checkbox"
              checked={showCancelled}
              onChange={(e) => setShowCancelled(e.target.checked)}
              className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
            />
            Show cancelled
          </label>
        </div>

        {/* Navigation */}
        <div className="flex items-center gap-2">
          <button
            onClick={viewMode === "week" ? prevWeek : prevMonth}
            className="p-1.5 rounded-lg hover:bg-gray-100"
          >
            <ChevronLeft className="w-5 h-5 text-gray-600" />
          </button>
          <h2 className="text-sm font-semibold text-gray-900 min-w-[200px] text-center">
            {viewMode === "week" ? weekLabel : `${MONTH_NAMES[month]} ${year}`}
          </h2>
          <button
            onClick={viewMode === "week" ? nextWeek : nextMonth}
            className="p-1.5 rounded-lg hover:bg-gray-100"
          >
            <ChevronRight className="w-5 h-5 text-gray-600" />
          </button>
        </div>

        <button onClick={goToday} className="text-xs text-brand-600 hover:text-brand-700 font-medium">
          Today
        </button>
      </div>

      {/* Legend */}
      <div className="flex gap-3 mb-4 flex-wrap items-center text-xs text-gray-500">
        <div className="flex items-center gap-1.5"><div className="w-2.5 h-3 rounded-sm" style={{ backgroundColor: "#82A53F" }} /><span>Course colour = type</span></div>
        <div className="flex items-center gap-1.5"><div className="w-3.5 h-3.5 rounded-full flex items-center justify-center text-white text-[8px] font-bold" style={{ backgroundColor: "#E8792B" }}>AB</div><span>Alex</span></div>
        <div className="flex items-center gap-1.5"><div className="w-3.5 h-3.5 rounded-full flex items-center justify-center text-white text-[8px] font-bold" style={{ backgroundColor: "#6366F1" }}>CB</div><span>Chris</span></div>
        <span className="text-gray-400">▲ = at risk</span>
        <span className="text-gray-400">P = private</span>
        <div className="flex items-center gap-1"><span className="bg-blue-100 text-blue-600 text-[9px] font-bold rounded px-0.5">V</span><span>virtual</span></div>
        <div className="flex items-center gap-1"><span className="bg-gray-100 text-gray-600 text-[9px] font-bold rounded px-0.5">F2F</span><span>in-person</span></div>
        <span className="text-gray-400">enrolled/{minEnrolments} = enrolments/min</span>
      </div>

      {/* ── WEEK VIEW ───────────────────────────────────────────────────────── */}
      {viewMode === "week" && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          {/* Day headers */}
          <div className="grid grid-cols-7 border-b border-gray-200 bg-gray-50">
            {weekDays.map((d, i) => {
              const ds = toDateStr(d);
              const isToday = ds === todayStr;
              return (
                <div key={i} className={`px-2 py-2 text-center ${isToday ? "bg-blue-50" : ""}`}>
                  <p className="text-xs font-medium text-gray-500 uppercase">{DAY_NAMES[i]}</p>
                  <p className={`text-sm font-semibold mt-0.5
                    ${isToday ? "bg-blue-600 text-white rounded-full w-7 h-7 flex items-center justify-center mx-auto" : "text-gray-700"}`}>
                    {d.getDate()}
                  </p>
                  <p className="text-[10px] text-gray-400">{MONTH_NAMES[d.getMonth()].slice(0, 3)}</p>
                </div>
              );
            })}
          </div>

          {/* Course rows */}
          <div className="grid grid-cols-7 min-h-[160px]">
            {weekDays.map((d, i) => {
              const ds = toDateStr(d);
              const isToday = ds === todayStr;
              const isWeekend = i >= 5;
              const dayCourses = coursesOnDate(ds);

              return (
                <div
                  key={i}
                  className={`p-1.5 border-r border-gray-100 last:border-r-0 space-y-0.5
                    ${isWeekend ? "bg-gray-50/40" : ""}
                    ${isToday ? "bg-blue-50/30" : ""}`}
                >
                  {loading && i === 0 && (
                    <p className="text-xs text-gray-400 col-span-7 p-2">Loading...</p>
                  )}
                  {dayCourses.map((c) => (
                    <CourseTile key={c.id} course={c} minEnrolments={minEnrolments} cellDate={ds} />
                  ))}
                  {dayCourses.length === 0 && !loading && (
                    <div className="h-full" />
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* ── MONTH VIEW ──────────────────────────────────────────────────────── */}
      {viewMode === "month" && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          {/* Day headers */}
          <div className="grid grid-cols-7 border-b border-gray-200 bg-gray-50">
            {DAY_NAMES.map((d) => (
              <div key={d} className="px-2 py-2 text-xs font-medium text-gray-500 text-center uppercase">{d}</div>
            ))}
          </div>

          {/* Weeks */}
          {weeks.map((week, wi) => (
            <div key={wi} className="grid grid-cols-7 border-b border-gray-100 last:border-b-0">
              {week.map((day, di) => {
                if (day === null) return <div key={di} className="min-h-[80px] md:min-h-[100px] bg-gray-50/50" />;

                const dateStr = `${year}-${String(month + 1).padStart(2, "0")}-${String(day).padStart(2, "0")}`;
                const dayCourses = coursesOnDate(dateStr);
                const isToday = dateStr === todayStr;
                const isWeekend = di >= 5;

                return (
                  <div key={di} className={`min-h-[80px] md:min-h-[100px] p-1 border-r border-gray-100 last:border-r-0
                    ${isWeekend ? "bg-gray-50/30" : ""}
                    ${isToday ? "bg-blue-50/50" : ""}`}>
                    <div className={`text-xs font-medium mb-1 px-1
                      ${isToday
                        ? "bg-blue-600 text-white rounded-full w-6 h-6 flex items-center justify-center"
                        : "text-gray-500"}`}>
                      {day}
                    </div>
                    <div className="space-y-0.5">
                      {dayCourses.slice(0, 3).map((c) => (
                        <CourseTile key={c.id} course={c} minEnrolments={minEnrolments} compact cellDate={dateStr} />
                      ))}
                      {dayCourses.length > 3 && (
                        <p className="text-[10px] text-gray-400 px-1">+{dayCourses.length - 3} more</p>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          ))}
        </div>
      )}
    </>
  );
}
