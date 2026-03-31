"use client";

import { useEffect, useState, useCallback } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { CourseScheduleItem, getCourseSchedules } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { Badge } from "@/components/ui/Badge";
import { Button } from "@/components/ui/Button";
import { ChevronLeft, ChevronRight, List } from "lucide-react";
import Link from "next/link";

const DAY_NAMES = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"];
const MONTH_NAMES = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"];

type Status = "running" | "completed" | "at risk" | "guaranteed" | "monitor" | "cancelled";

const STATUS_COLORS: Record<Status, string> = {
  running: "bg-blue-500 text-white",
  completed: "bg-gray-300 text-gray-700",
  "at risk": "bg-red-500 text-white",
  guaranteed: "bg-green-500 text-white",
  monitor: "bg-amber-400 text-amber-900",
  cancelled: "bg-gray-200 text-gray-400 line-through",
};

export default function CalendarPage() {
  const apiKey = useApiKey();
  const [courses, setCourses] = useState<CourseScheduleItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const now = new Date();
  const [year, setYear] = useState(now.getFullYear());
  const [month, setMonth] = useState(now.getMonth()); // 0-indexed

  const loadCourses = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);
    setError("");
    // Load 2 months of data for overlap
    const from = new Date(year, month - 1, 1).toISOString().split("T")[0];
    const to = new Date(year, month + 2, 0).toISOString().split("T")[0];
    try {
      const result = await getCourseSchedules(apiKey, { from, to, pageSize: 100 });
      setCourses((result.items || []).filter((c) => c.title && c.startDate));
    } catch {
      setError("Failed to load courses");
    } finally {
      setLoading(false);
    }
  }, [apiKey, year, month]);

  useEffect(() => { loadCourses(); }, [loadCourses]);

  function prevMonth() {
    if (month === 0) { setMonth(11); setYear(year - 1); }
    else setMonth(month - 1);
  }
  function nextMonth() {
    if (month === 11) { setMonth(0); setYear(year + 1); }
    else setMonth(month + 1);
  }
  function goToday() { setYear(now.getFullYear()); setMonth(now.getMonth()); }

  const today = new Date(); today.setHours(0, 0, 0, 0);

  function getStatus(c: CourseScheduleItem): Status {
    if (c.status === "cancelled") return "cancelled";
    const start = new Date(c.startDate || ""); start.setHours(0, 0, 0, 0);
    const end = c.endDate ? new Date(c.endDate) : start; end.setHours(0, 0, 0, 0);
    if (start <= today && today <= end) return "running";
    if (today > end) return "completed";
    if (c.needsAttention) return "at risk";
    return c.guaranteedToRun ? "guaranteed" : "monitor";
  }

  // Build calendar grid
  const firstDay = new Date(year, month, 1);
  const lastDay = new Date(year, month + 1, 0);
  const startDow = (firstDay.getDay() + 6) % 7; // Monday=0
  const daysInMonth = lastDay.getDate();

  // Get courses for a specific date
  function coursesOnDate(dateStr: string) {
    return courses.filter((c) => {
      const start = (c.startDate || "").split("T")[0];
      const end = (c.endDate || c.startDate || "").split("T")[0];
      return dateStr >= start && dateStr <= end;
    });
  }

  // Build weeks
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

      {/* Month navigation */}
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <button onClick={prevMonth} className="p-1.5 rounded-lg hover:bg-gray-100"><ChevronLeft className="w-5 h-5 text-gray-600" /></button>
          <h2 className="text-lg font-semibold text-gray-900 min-w-[180px] text-center">
            {MONTH_NAMES[month]} {year}
          </h2>
          <button onClick={nextMonth} className="p-1.5 rounded-lg hover:bg-gray-100"><ChevronRight className="w-5 h-5 text-gray-600" /></button>
        </div>
        <button onClick={goToday} className="text-xs text-brand-600 hover:text-brand-700 font-medium">Today</button>
      </div>

      {/* Legend */}
      <div className="flex gap-3 mb-4 flex-wrap">
        {(Object.entries(STATUS_COLORS) as [Status, string][]).filter(([s]) => s !== "cancelled").map(([status, cls]) => (
          <div key={status} className="flex items-center gap-1.5">
            <div className={`w-3 h-3 rounded ${cls.split(" ")[0]}`} />
            <span className="text-xs text-gray-500 capitalize">{status}</span>
          </div>
        ))}
      </div>

      {/* Calendar grid */}
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
              const isToday = dateStr === today.toISOString().split("T")[0];
              const isWeekend = di >= 5;

              return (
                <div key={di} className={`min-h-[80px] md:min-h-[100px] p-1 border-r border-gray-100 last:border-r-0
                  ${isWeekend ? "bg-gray-50/30" : ""}
                  ${isToday ? "bg-blue-50/50" : ""}`}>
                  <div className={`text-xs font-medium mb-1 px-1 ${isToday ? "bg-blue-600 text-white rounded-full w-6 h-6 flex items-center justify-center" : "text-gray-500"}`}>
                    {day}
                  </div>
                  <div className="space-y-0.5">
                    {dayCourses.slice(0, 3).map((c) => {
                      const status = getStatus(c);
                      const colorClass = STATUS_COLORS[status] || STATUS_COLORS.monitor;
                      const code = c.courseCode?.split("-")[0] || "";
                      const trainer = c.trainerName?.split(" ").map((n) => n[0]).join("") || "";
                      return (
                        <Link key={c.id} href={`/courses/${c.id}`}
                          className={`block px-1.5 py-0.5 rounded text-[10px] md:text-xs font-medium truncate ${colorClass} hover:opacity-80 transition-opacity`}
                          title={`${c.title} — ${c.currentEnrolmentCount} enrolled`}>
                          <span className="hidden md:inline">{code}</span>
                          <span className="md:hidden">{code}</span>
                          {trainer && <span className="hidden md:inline text-[10px] opacity-75 ml-1">{trainer}</span>}
                          <span className="float-right opacity-75">{c.currentEnrolmentCount}</span>
                        </Link>
                      );
                    })}
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
    </>
  );
}
