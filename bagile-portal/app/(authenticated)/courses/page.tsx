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
import { GraduationCap, Plus, Search, List, CalendarDays } from "lucide-react";
import Link from "next/link";
import { getCourseDisplayStatus } from "@/lib/courseStatus";
import { CalendarView } from "@/components/courses/CalendarView";

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
  const [dateRange, setDateRange] = useState(
    urlType ? String(urlYear || currentYear) : "upcoming"
  );
  const [showCreate, setShowCreate] = useState(false);
  const [visibilityFilter, setVisibilityFilter] = useState("all");
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [sortAsc, setSortAsc] = useState(true); // earliest first by default

  // Page-level view: "list" or "calendar"
  const [pageView, setPageView] = useState<"list" | "calendar">(() => {
    if (typeof window !== "undefined") {
      return (localStorage.getItem("bagile_courses_page_view") as "list" | "calendar") || "list";
    }
    return "list";
  });

  function switchPageView(v: "list" | "calendar") {
    setPageView(v);
    if (typeof window !== "undefined") {
      localStorage.setItem("bagile_courses_page_view", v);
    }
  }

  // Calendar range — updated by CalendarView when user navigates
  const [calRange, setCalRange] = useState<{ from: string; to: string } | null>(null);

  // Debounce search
  useEffect(() => {
    const timer = setTimeout(() => setSearch(searchInput), 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const loadCourses = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);

    let from: string | undefined;
    let to: string | undefined;

    if (pageView === "calendar" && calRange) {
      from = calRange.from;
      to = calRange.to;
    } else if (dateRange === "upcoming") {
      const d = new Date(); d.setDate(d.getDate() - 2);
      from = d.toISOString().split("T")[0];
    } else if (dateRange !== "all") {
      // It's a year number
      from = `${dateRange}-01-01`;
      to = `${dateRange}-12-31`;
    }

    const type = visibilityFilter !== "all" ? visibilityFilter : undefined;

    try {
      const result = await getCourseSchedules(apiKey, {
        from, to, type, pageSize: 100,
      });
      setCourses((result.items || []).filter(
        (c) => c.title && c.title !== "N/A" && c.courseCode
      ));
    } catch {
      setCourses([]);
    } finally {
      setLoading(false);
    }
  }, [apiKey, dateRange, visibilityFilter, pageView, calRange]);

  useEffect(() => { loadCourses(); }, [loadCourses]);

  // Re-fetch when calendar range changes (user navigates weeks/months)
  useEffect(() => {
    if (pageView === "calendar" && calRange) loadCourses();
  }, [calRange]); // eslint-disable-line react-hooks/exhaustive-deps

  const courseTypes = [...new Set(
    courses.map((c) => c.courseCode?.split("-")[0]).filter(Boolean)
  )].sort() as string[];

  const today = new Date(); today.setHours(0, 0, 0, 0);
  const getDisplayStatus = (c: CourseScheduleItem) => getCourseDisplayStatus(c);

  const isVirtual = (c: CourseScheduleItem) =>
    c.formatType?.toLowerCase().includes("virtual") || c.location?.toLowerCase().includes("virtual");

  // Filter by search + sort by date
  // Course code matching uses segment-aware logic (split by '-') so that
  // e.g. "PSPOA" matches PSPOA-xxx but NOT PSPOAI-xxx, and "PSMA" doesn't match PSMAI.
  // If the search matches a known course type exactly, only that type matches.
  // Otherwise partial prefix search on the type segment is allowed.
  const matchesCourseCode = (code: string, term: string) => {
    const segments = code.toLowerCase().split("-");
    const typeSegment = segments[0];
    const lowerTerm = term.toLowerCase();
    const isKnownType = courseTypes.some((t) => t.toLowerCase() === lowerTerm);
    if (isKnownType) return typeSegment === lowerTerm;
    if (typeSegment.startsWith(lowerTerm)) return true;
    return segments.slice(1).some((s) => s.includes(lowerTerm));
  };

  const filtered = courses
    .filter((c) => typeFilter === "all" || c.courseCode.split("-")[0] === typeFilter)
    .filter((c) => !search || c.title.toLowerCase().includes(search.toLowerCase()) || matchesCourseCode(c.courseCode, search))
    .sort((a, b) => {
      const da = new Date(a.startDate || "").getTime();
      const db = new Date(b.startDate || "").getTime();
      return sortAsc ? da - db : db - da;
    });

  const totalAttendees = filtered.reduce((s, c) => s + c.currentEnrolmentCount, 0);

  // Segmented date range options
  const dateOptions = [
    { value: "upcoming", label: "Upcoming" },
    { value: String(currentYear), label: String(currentYear) },
    { value: String(currentYear - 1), label: String(currentYear - 1) },
    { value: "all", label: "All" },
  ];

  return (
    <>
      <CreatePrivateCoursePanel
        open={showCreate}
        onClose={() => setShowCreate(false)}
        apiKey={apiKey}
        onCreated={loadCourses}
      />

      <PageHeader
        title={`Courses${!loading && pageView === "list" ? ` (${filtered.length})` : ""}`}
        subtitle={pageView === "list" && typeFilter !== "all" ? `${typeFilter} — ${totalAttendees} attendees` : undefined}
        actions={
          <div className="flex gap-2">
            {/* List / Calendar toggle */}
            <div className="flex rounded-lg border border-gray-300 overflow-hidden">
              <button
                onClick={() => switchPageView("list")}
                className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium transition-colors
                  ${pageView === "list" ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
              >
                <List className="w-3.5 h-3.5" /> List
              </button>
              <button
                onClick={() => switchPageView("calendar")}
                className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium border-l border-gray-300 transition-colors
                  ${pageView === "calendar" ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
              >
                <CalendarDays className="w-3.5 h-3.5" /> Calendar
              </button>
            </div>
            <Button onClick={() => setShowCreate(true)}>
              <Plus className="w-4 h-4" /> Create Private Course
            </Button>
          </div>
        }
      />

      {/* Calendar view — inline, no page navigation */}
      {pageView === "calendar" && (
        <CalendarView
          courses={courses}
          loading={loading}
          storageKey="bagile_courses_cal_view"
          onRangeChange={(from, to) => setCalRange({ from, to })}
        />
      )}

      {/* List view — filters + table */}
      {pageView === "list" && (<>

      {/* Search + filters */}
      <div className="flex gap-3 mb-4 items-center flex-wrap">
        {/* Search */}
        <div className="relative flex-1 min-w-[200px] max-w-xs">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Search courses..."
            value={searchInput}
            onChange={(e) => setSearchInput(e.target.value)}
            className="w-full pl-10 pr-3 py-1.5 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
          />
        </div>

        {/* Date range — segmented */}
        <div className="inline-flex rounded-lg border border-gray-300 overflow-hidden">
          {dateOptions.map((opt) => (
            <button
              key={opt.value}
              onClick={() => setDateRange(opt.value)}
              className={`px-3 py-1.5 text-xs font-medium transition-colors ${
                dateRange === opt.value
                  ? "bg-brand-600 text-white"
                  : "bg-white text-gray-600 hover:bg-gray-50"
              } ${opt.value !== dateOptions[0].value ? "border-l border-gray-300" : ""}`}
            >
              {opt.label}
            </button>
          ))}
        </div>

        {/* Visibility toggle */}
        <div className="inline-flex rounded-lg border border-gray-300 overflow-hidden">
          {[
            { value: "all", label: "All" },
            { value: "public", label: "Public" },
            { value: "private", label: "Private" },
          ].map((opt) => (
            <button
              key={opt.value}
              onClick={() => setVisibilityFilter(opt.value)}
              className={`px-3 py-1.5 text-xs font-medium transition-colors ${
                visibilityFilter === opt.value
                  ? "bg-brand-600 text-white"
                  : "bg-white text-gray-600 hover:bg-gray-50"
              } ${opt.value !== "all" ? "border-l border-gray-300" : ""}`}
            >
              {opt.label}
            </button>
          ))}
        </div>

        {/* Course type */}
        {courseTypes.length > 1 && (
          <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
            <option value="all">All types</option>
            {urlType && !courseTypes.includes(urlType) && <option value={urlType}>{urlType}</option>}
            {courseTypes.map((t) => <option key={t} value={t}>{t}</option>)}
          </select>
        )}
      </div>

      {/* Sort toggle */}
      <div className="flex items-center justify-between mb-2">
        <button onClick={() => setSortAsc(!sortAsc)} className="text-xs text-gray-500 hover:text-gray-700 flex items-center gap-1">
          Sort by date {sortAsc ? "↑ earliest first" : "↓ latest first"}
        </button>
        {!loading && filtered.length > 0 && (
          <span className="text-xs text-gray-400">{filtered.length} course{filtered.length !== 1 ? "s" : ""} · {totalAttendees} attendees</span>
        )}
      </div>

      {/* Mobile cards */}
      <div className="md:hidden space-y-3">
        {loading && Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="bg-white rounded-xl border border-gray-200 p-4">
            <div className="skeleton h-4 w-3/4 mb-2 rounded" />
            <div className="skeleton h-3 w-1/2 mb-3 rounded" />
            <div className="flex gap-2"><div className="skeleton h-6 w-12 rounded" /><div className="skeleton h-6 w-16 rounded" /></div>
          </div>
        ))}
        {!loading && filtered.map((c) => {
          const status = getDisplayStatus(c);
          const daysAway = c.startDate ? Math.round((new Date(c.startDate).getTime() - today.getTime()) / (1000 * 60 * 60 * 24)) : 0;
          const daysLabel = daysAway > 0 ? `${daysAway}d away` : daysAway === 0 ? "today" : `${Math.abs(daysAway)}d ago`;
          return (
            <div key={c.id}
              onClick={() => window.location.href = `/courses/${c.id}`}
              className={`bg-white rounded-xl border border-gray-200 shadow-sm p-4 cursor-pointer hover:bg-gray-50 transition-colors
                ${status === "cancel" ? "border-red-300 bg-red-100/50" : status === "at risk" ? "border-red-200 bg-red-50/30" : ""}
                ${status === "completed" ? "opacity-60" : ""}`}>
              <div className="flex items-start justify-between">
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-gray-900 truncate">{c.title}</p>
                  <p className="text-xs text-gray-400 font-mono mt-0.5">{c.courseCode}</p>
                </div>
                <div className="ml-3 text-right shrink-0">
                  <span className={`text-xl font-bold ${
                    status === "cancel" || status === "at risk" ? "text-red-600" :
                    status === "guaranteed" || status === "running" || status === "completed" ? "text-green-600" : "text-gray-700"
                  }`}>{c.currentEnrolmentCount}</span>
                  {c.capacity && <span className="text-xs text-gray-400">/{c.capacity}</span>}
                </div>
              </div>
              <div className="flex items-center gap-2 mt-2 flex-wrap">
                <span className="text-sm text-gray-600">{formatDate(c.startDate)} ({daysLabel})</span>
                {statusBadge(status)}
                <Badge variant={isVirtual(c) ? "info" : "neutral"}>
                  {isVirtual(c) ? "Virtual" : "In-person"}
                </Badge>
                {c.type === "private" && <Badge variant="warning">Private</Badge>}
              </div>
            </div>
          );
        })}
        {!loading && filtered.length === 0 && (
          <EmptyState
            icon={<GraduationCap className="w-10 h-10" />}
            title="No courses"
            description={search ? `No courses matching "${search}"` : "No courses match your filters"}
          />
        )}
      </div>

      {/* Desktop table */}
      <div className="hidden md:block bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Course</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Date</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Trainer</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Enrolled</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 8 }).map((_, i) => <SkeletonRow key={i} cols={5} />)}
            {!loading && filtered.map((c) => {
              const status = getDisplayStatus(c);
              const daysAway = c.startDate ? Math.round((new Date(c.startDate).getTime() - today.getTime()) / (1000 * 60 * 60 * 24)) : 0;
              const daysLabel = daysAway > 0 ? `${daysAway}d away` : daysAway === 0 ? "today" : `${Math.abs(daysAway)}d ago`;
              return (
                <tr key={c.id}
                  className={`border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors
                    ${status === "cancel" ? "bg-red-100/60" : status === "at risk" ? "bg-red-50/50" : ""}
                    ${status === "completed" ? "opacity-60" : ""}`}
                  onClick={() => window.location.href = `/courses/${c.id}`}>
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-900">{c.title}</p>
                    <div className="flex items-center gap-2 mt-0.5">
                      <span className="text-xs text-gray-400 font-mono">{c.courseCode}</span>
                      <Badge variant={isVirtual(c) ? "info" : "neutral"}>
                        {isVirtual(c) ? "Virtual" : "In-person"}
                      </Badge>
                      {c.type === "private" && <Badge variant="warning">Private</Badge>}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-gray-600 whitespace-nowrap">
                    {formatDate(c.startDate)} <span className="text-xs text-gray-400">({daysLabel})</span>
                  </td>
                  <td className="px-4 py-3 text-gray-600">{c.trainerName || "—"}</td>
                  <td className="px-4 py-3 text-center">
                    <span className={`text-lg font-bold ${
                      status === "cancel" || status === "at risk" ? "text-red-600" :
                      status === "guaranteed" || status === "running" || status === "completed" ? "text-green-600" :
                      "text-gray-700"
                    }`}>{c.currentEnrolmentCount}</span>
                    {c.capacity && <span className="text-xs text-gray-400">/{c.capacity}</span>}
                  </td>
                  <td className="px-4 py-3 text-center">{statusBadge(status)}</td>
                </tr>
              );
            })}
            {!loading && filtered.length === 0 && (
              <tr><td colSpan={5}>
                <EmptyState
                  icon={<GraduationCap className="w-10 h-10" />}
                  title="No courses"
                  description={search ? `No courses matching "${search}"` : "No courses match your filters"}
                />
              </td></tr>
            )}
          </tbody>
        </table>
      </div>

      </>)} {/* end pageView === "list" */}
    </>
  );
}
