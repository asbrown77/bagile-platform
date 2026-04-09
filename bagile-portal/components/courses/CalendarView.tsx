"use client";

/**
 * CalendarView — embeddable calendar grid for courses.
 *
 * Extracted from /courses/calendar/page.tsx so it can be used both
 * as a standalone page and inline on the courses list page.
 *
 * Receives already-fetched courses; all navigation, filtering, and
 * date-range state lives here so callers just pass courses + config.
 */

import { useState, useEffect, useRef } from "react";
import { CourseScheduleItem } from "@/lib/api";
import { ChevronLeft, ChevronRight, CalendarDays, CalendarRange } from "lucide-react";
import Link from "next/link";
import { getCourseDisplayStatus } from "@/lib/courseStatus";
import { loadConfig } from "@/lib/config";
import { getCourseColour, getTrainerColour, trainerInitials } from "@/lib/courseColours";

const DAY_NAMES = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
const MONTH_NAMES = [
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December",
];

export type CalViewMode = "week" | "month";

const STATUS_BG: Record<string, string> = {
  running:    "bg-blue-50 border-gray-200",
  completed:  "bg-gray-50 border-gray-200",
  cancel:     "bg-red-50 border-red-200",
  "at risk":  "bg-red-50 border-red-200",
  guaranteed: "bg-green-50 border-gray-200",
  monitor:    "bg-amber-50 border-gray-200",
  cancelled:  "bg-gray-50 border-gray-100 opacity-50",
};

/** Format a Date as YYYY-MM-DD using local time (not UTC) to avoid off-by-one
 *  errors in BST/CET where midnight local = prior day in UTC. */
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

function getWeekStart(d: Date): Date {
  const day = d.getDay();
  const diff = day === 0 ? -6 : 1 - day;
  const monday = new Date(d);
  monday.setDate(d.getDate() + diff);
  monday.setHours(0, 0, 0, 0);
  return monday;
}

interface CourseTileProps {
  course: CourseScheduleItem;
  minEnrolments: number;
  compact?: boolean;
  cellDate?: string;
}

function CourseTile({ course, minEnrolments, compact = false, cellDate }: CourseTileProps) {
  const status = getCourseDisplayStatus(course);
  const bgClass = STATUS_BG[status] || STATUS_BG.monitor;
  const isCancelled = status === "cancelled";
  const isAtRisk = status === "at risk" || status === "cancel";
  const isPrivate = course.type === "private";
  const isVirtual = course.formatType?.toLowerCase().includes("virtual") ?? false;

  const firstSegment = course.courseCode?.split("-")[0] || "";
  const borderColour = getCourseColour(course.courseCode || "");
  const initials = trainerInitials(course.trainerName);
  const avatarColour = getTrainerColour(initials);

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
        <div className="flex items-center gap-1 min-w-0">
          <span className={`font-semibold truncate text-gray-800 ${isCancelled ? "line-through" : ""}`}>
            {isMultiDay ? `${firstSegment} (${dayIndex}/${spanDays})` : firstSegment}
          </span>
          {isPrivate && (
            <span className="shrink-0 text-[9px] bg-amber-100 text-amber-700 rounded px-1 py-0 leading-tight font-bold">P</span>
          )}
        </div>

        <div className="flex items-center gap-1 mt-0.5 flex-wrap">
          {course.trainerName && (
            <span
              className="inline-flex items-center justify-center rounded-full text-white font-bold leading-none shrink-0"
              style={{ backgroundColor: avatarColour, width: 14, height: 14, fontSize: 8 }}
            >
              {initials}
            </span>
          )}
          {/* Format badge */}
          <span
            className={`shrink-0 text-[9px] font-bold rounded px-0.5 leading-tight ${
              isVirtual ? "bg-blue-100 text-blue-600" : "bg-gray-100 text-gray-600"
            }`}
          >
            {isVirtual ? "V" : "F2F"}
          </span>
          {isAtRisk && (
            <span className="shrink-0 text-amber-500" title="At risk / cancel">▲</span>
          )}
          {!compact && <span className="text-gray-500 shrink-0">{enrolDisplay}</span>}
          {compact && <span className="text-gray-500 shrink-0">{course.currentEnrolmentCount}</span>}
          {!compact && dateLabel && (
            <span className="text-gray-400 shrink-0 hidden sm:inline">{dateLabel}</span>
          )}
        </div>
      </div>
    </Link>
  );
}

export interface CalendarViewProps {
  /** All courses for the currently visible range — caller is responsible for fetching. */
  courses: CourseScheduleItem[];
  loading?: boolean;
  /** Persist view-mode key in localStorage under this key. */
  storageKey?: string;
  /** Initial view mode (before localStorage is read). */
  defaultView?: CalViewMode;
  /** Called when date range changes so the parent can re-fetch if needed. */
  onRangeChange?: (from: string, to: string) => void;
}

export function CalendarView({
  courses,
  loading = false,
  storageKey = "bagile_cal_view",
  defaultView = "month",
  onRangeChange,
}: CalendarViewProps) {
  const now = new Date();
  const [viewMode, setViewMode] = useState<CalViewMode>(defaultView);
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth());
  const [weekStart, setWeekStart] = useState(() => getWeekStart(now));
  const [trainerFilter, setTrainerFilter] = useState<"AB" | "CB" | "all">("all");
  const [showCancelled, setShowCancelled] = useState(false);
  const { minEnrolments } = loadConfig();

  // Stable ref for onRangeChange — avoids the infinite loop where a new arrow
  // function on every parent render would re-trigger this effect, which would
  // trigger a state update, which would re-render the parent... 188+ times.
  const onRangeChangeRef = useRef(onRangeChange);
  useEffect(() => { onRangeChangeRef.current = onRangeChange; });

  // Restore persisted view mode after mount
  useEffect(() => {
    if (typeof window !== "undefined") {
      const stored = localStorage.getItem(storageKey) as CalViewMode | null;
      if (stored === "week" || stored === "month") setViewMode(stored);
    }
  }, [storageKey]);

  // Notify parent when date range changes so it can re-fetch.
  // Depends only on the date/view state — NOT on onRangeChange (use ref instead)
  // to avoid the re-render → new function → re-effect infinite loop.
  useEffect(() => {
    if (!onRangeChangeRef.current) return;
    let from: string;
    let to: string;
    if (viewMode === "week") {
      from = toDateStr(addDays(weekStart, -1));
      to = toDateStr(addDays(weekStart, 8));
    } else {
      from = new Date(year, month - 1, 1).toISOString().split("T")[0];
      to = new Date(year, month + 2, 0).toISOString().split("T")[0];
    }
    onRangeChangeRef.current(from, to);
  }, [viewMode, year, month, weekStart]); // eslint-disable-line react-hooks/exhaustive-deps

  function switchView(v: CalViewMode) {
    setViewMode(v);
    if (typeof window !== "undefined") localStorage.setItem(storageKey, v);
  }

  const today = new Date(); today.setHours(0, 0, 0, 0);
  const todayStr = toDateStr(today);

  function coursesOnDate(dateStr: string) {
    return courses.filter((c) => {
      const start = (c.startDate || "").split("T")[0];
      const end = (c.endDate || c.startDate || "").split("T")[0];
      if (dateStr < start || dateStr > end) return false;
      if (!showCancelled && getCourseDisplayStatus(c) === "cancelled") return false;
      if (trainerFilter !== "all") {
        if (trainerInitials(c.trainerName) !== trainerFilter) return false;
      }
      return true;
    });
  }

  // Month grid
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const startDow = (firstDay.getDay() + 6) % 7;
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

  // Week view
  const weekDays = Array.from({ length: 7 }, (_, i) => addDays(weekStart, i));
  const weekEndDate = addDays(weekStart, 6);
  const weekLabel = weekStart.getMonth() === weekEndDate.getMonth()
    ? `${MONTH_NAMES[weekStart.getMonth()]} ${weekStart.getDate()}–${weekEndDate.getDate()}, ${weekStart.getFullYear()}`
    : `${MONTH_NAMES[weekStart.getMonth()]} ${weekStart.getDate()} – ${MONTH_NAMES[weekEndDate.getMonth()]} ${weekEndDate.getDate()}, ${weekStart.getFullYear()}`;

  return (
    <div>
      {/* Toolbar */}
      <div className="flex items-center justify-between mb-3 gap-2 flex-wrap">
        <div className="flex items-center gap-2 flex-wrap">
          {/* Week/Month toggle */}
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
            onClick={() => {
              if (viewMode === "week") setWeekStart((ws) => addDays(ws, -7));
              else if (month === 0) { setMonth(11); setYear(year - 1); }
              else setMonth(month - 1);
            }}
            className="p-1.5 rounded-lg hover:bg-gray-100"
          >
            <ChevronLeft className="w-5 h-5 text-gray-600" />
          </button>
          <h2 className="text-sm font-semibold text-gray-900 min-w-[200px] text-center">
            {viewMode === "week" ? weekLabel : `${MONTH_NAMES[month]} ${year}`}
          </h2>
          <button
            onClick={() => {
              if (viewMode === "week") setWeekStart((ws) => addDays(ws, 7));
              else if (month === 11) { setMonth(0); setYear(year + 1); }
              else setMonth(month + 1);
            }}
            className="p-1.5 rounded-lg hover:bg-gray-100"
          >
            <ChevronRight className="w-5 h-5 text-gray-600" />
          </button>
        </div>

        <button
          onClick={() => {
            const t = new Date();
            setYear(t.getFullYear());
            setMonth(t.getMonth());
            setWeekStart(getWeekStart(t));
          }}
          className="text-xs text-brand-600 hover:text-brand-700 font-medium"
        >
          Today
        </button>
      </div>

      {/* Legend */}
      <div className="flex gap-3 mb-3 flex-wrap items-center text-xs text-gray-500">
        <div className="flex items-center gap-1.5"><div className="w-2.5 h-3 rounded-sm" style={{ backgroundColor: "#82A53F" }} /><span>Course colour = type</span></div>
        <div className="flex items-center gap-1.5"><div className="w-3.5 h-3.5 rounded-full flex items-center justify-center text-white text-[8px] font-bold" style={{ backgroundColor: "#E8792B" }}>AB</div><span>Alex</span></div>
        <div className="flex items-center gap-1.5"><div className="w-3.5 h-3.5 rounded-full flex items-center justify-center text-white text-[8px] font-bold" style={{ backgroundColor: "#6366F1" }}>CB</div><span>Chris</span></div>
        <span className="text-gray-400">▲ = at risk</span>
        <span className="text-gray-400">P = private</span>
        <div className="flex items-center gap-1"><span className="bg-blue-100 text-blue-600 text-[9px] font-bold rounded px-0.5">V</span><span>virtual</span></div>
        <div className="flex items-center gap-1"><span className="bg-gray-100 text-gray-600 text-[9px] font-bold rounded px-0.5">F2F</span><span>in-person</span></div>
        <span className="text-gray-400">enrolled/{minEnrolments} = enrolments/min</span>
      </div>

      {/* Week view */}
      {viewMode === "week" && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
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
                    <p className="text-xs text-gray-400 p-2">Loading...</p>
                  )}
                  {dayCourses.map((c) => (
                    <CourseTile key={c.id} course={c} minEnrolments={minEnrolments} cellDate={ds} />
                  ))}
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* Month view */}
      {viewMode === "month" && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <div className="grid grid-cols-7 border-b border-gray-200 bg-gray-50">
            {DAY_NAMES.map((d) => (
              <div key={d} className="px-2 py-2 text-xs font-medium text-gray-500 text-center uppercase">{d}</div>
            ))}
          </div>
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
    </div>
  );
}
