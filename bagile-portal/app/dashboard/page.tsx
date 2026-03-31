"use client";

import { useEffect, useState } from "react";
import { MonitoringCourse, RevenueData, getMonitoring, getRevenue, getPendingTransfers } from "@/lib/api";

const API_KEY = process.env.NEXT_PUBLIC_BAGILE_API_KEY || "";

export default function Dashboard() {
  const [courses, setCourses] = useState<MonitoringCourse[]>([]);
  const [revenue, setRevenue] = useState<RevenueData | null>(null);
  const [pendingTransferCount, setPendingTransferCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [trainerFilter, setTrainerFilter] = useState("all");
  const [statusFilter, setStatusFilter] = useState("all");
  const [courseTypeFilter, setCourseTypeFilter] = useState("all");

  useEffect(() => {
    if (!API_KEY) {
      setError("API key not configured");
      setLoading(false);
      return;
    }
    loadData();
  }, []);

  async function loadData() {
    try {
      const [monitoring, rev] = await Promise.all([
        getMonitoring(API_KEY, 60),
        getRevenue(API_KEY),
      ]);
      setCourses(monitoring);
      setRevenue(rev);
      // Pending transfers — don't break dashboard if this fails
      try {
        const pending = await getPendingTransfers(API_KEY);
        setPendingTransferCount(Array.isArray(pending) ? pending.length : 0);
      } catch { /* non-critical */ }
    } catch {
      setError("Failed to load data");
    } finally {
      setLoading(false);
    }
  }

  const trainers = [...new Set(courses.map((c) => c.trainerName).filter(Boolean))] as string[];
  const courseTypes = [...new Set(courses.map((c) => {
    const code = c.courseCode?.split("-")[0] || "";
    return code;
  }).filter(Boolean))].sort();

  const today = new Date(); today.setHours(0,0,0,0);
  const isRunning = (c: MonitoringCourse) => {
    const start = new Date(c.startDate); start.setHours(0,0,0,0);
    const end = c.endDate ? new Date(c.endDate) : start; end.setHours(0,0,0,0);
    return start <= today && today <= end;
  };
  const isCompleted = (c: MonitoringCourse) => {
    const end = c.endDate ? new Date(c.endDate) : new Date(c.startDate);
    end.setHours(0,0,0,0);
    return today > end;
  };
  const isAtRisk = (c: MonitoringCourse) => !isRunning(c) && !isCompleted(c) && c.currentEnrolmentCount <= 2 && c.daysUntilStart <= 7;
  const getStatus = (c: MonitoringCourse) => {
    if (isRunning(c)) return "running";
    if (isCompleted(c)) return "completed";
    if (isAtRisk(c)) return "at risk";
    return "healthy";
  };

  const filtered = courses.filter((c) => {
    if (trainerFilter !== "all" && c.trainerName !== trainerFilter) return false;
    if (courseTypeFilter !== "all" && !c.courseCode?.startsWith(courseTypeFilter)) return false;
    if (statusFilter === "at-risk" && !isAtRisk(c)) return false;
    if (statusFilter === "healthy" && isAtRisk(c)) return false;
    return true;
  });

  const atRiskCount = courses.filter(isAtRisk).length;

  return (
    <div className="max-w-6xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">BAgile</h1>
        <div className="flex gap-4 items-center text-sm">
          <span className="font-medium border-b-2 border-blue-600 pb-0.5">Dashboard</span>
          <a href="/settings" className="text-blue-600 hover:text-blue-800">MCP Keys</a>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>}

      {loading ? (
        <p className="text-gray-500">Loading...</p>
      ) : (
        <>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
            <Card label="Upcoming Courses" value={courses.length} />
            <Card label="At Risk" value={atRiskCount} color={atRiskCount > 0 ? "red" : "green"} />
            <Card label="Revenue This Month" value={revenue ? `£${revenue.thisMonth.total.toLocaleString()}` : "—"} />
            <Card label="Revenue This Year" value={revenue ? `£${revenue.thisYear.total.toLocaleString()}` : "—"} />
          </div>

          {pendingTransferCount > 0 && (
            <div className="bg-amber-50 border border-amber-200 rounded-lg p-3 mb-6 text-sm text-amber-800">
              <strong>{pendingTransferCount}</strong> attendee{pendingTransferCount !== 1 ? "s" : ""} pending transfer
            </div>
          )}

          {/* Filters */}
          <div className="flex gap-4 mb-4 items-center flex-wrap">
            {trainers.length > 1 && (
              <div className="flex gap-2 items-center">
                <span className="text-sm text-gray-600">Trainer:</span>
                <select value={trainerFilter} onChange={(e) => setTrainerFilter(e.target.value)} className="border rounded px-3 py-1.5 text-sm bg-white">
                  <option value="all">All</option>
                  {trainers.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
            )}
            {courseTypes.length > 1 && (
              <div className="flex gap-2 items-center">
                <span className="text-sm text-gray-600">Course:</span>
                <select value={courseTypeFilter} onChange={(e) => setCourseTypeFilter(e.target.value)} className="border rounded px-3 py-1.5 text-sm bg-white">
                  <option value="all">All</option>
                  {courseTypes.map((t) => <option key={t} value={t}>{t}</option>)}
                </select>
              </div>
            )}
            <div className="flex gap-2 items-center">
              <span className="text-sm text-gray-600">Status:</span>
              <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} className="border rounded px-3 py-1.5 text-sm bg-white">
                <option value="all">All</option>
                <option value="at-risk">At Risk</option>
                <option value="healthy">Healthy</option>
              </select>
            </div>
            <span className="text-sm text-gray-400">{filtered.length} course{filtered.length !== 1 ? "s" : ""}</span>
          </div>

          {/* Course table */}
          <div className="bg-white rounded-lg shadow-sm border overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-left px-4 py-2 font-medium text-gray-600">Course</th>
                  <th className="text-left px-4 py-2 font-medium text-gray-600">Date</th>
                  <th className="text-left px-4 py-2 font-medium text-gray-600">Trainer</th>
                  <th className="text-center px-4 py-2 font-medium text-gray-600">Enrolled</th>
                  <th className="text-center px-4 py-2 font-medium text-gray-600">Status</th>
                  <th className="text-left px-4 py-2 font-medium text-gray-600">Action</th>
                </tr>
              </thead>
              <tbody>
                {filtered.map((c) => (
                  <tr key={c.id}
                    className={`border-t hover:bg-gray-50 cursor-pointer ${isAtRisk(c) ? "bg-red-50/50" : isCompleted(c) ? "bg-gray-50/50" : ""}`}
                    onClick={() => window.location.href = `/course/${c.id}`}
                  >
                    <td className="px-4 py-3">
                      <p className="font-medium text-gray-900">{c.title}</p>
                      <p className="text-xs text-gray-500">{c.courseCode}</p>
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      {new Date(c.startDate).toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" })}
                      <p className="text-xs text-gray-400">{c.daysUntilStart}d away</p>
                    </td>
                    <td className="px-4 py-3 text-gray-600">{c.trainerName || "—"}</td>
                    <td className="px-4 py-3 text-center">
                      <span className={`font-bold ${isAtRisk(c) ? "text-red-600" : "text-green-600"}`}>
                        {c.currentEnrolmentCount}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      {(() => { const s = getStatus(c); return (
                        <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                          s === "running" ? "bg-blue-100 text-blue-700" :
                          s === "completed" ? "bg-gray-100 text-gray-600" :
                          s === "at risk" ? "bg-red-100 text-red-700" :
                          "bg-green-100 text-green-700"
                        }`}>{s}</span>
                      ); })()}
                    </td>
                    <td className="px-4 py-3 text-xs text-gray-600">{c.recommendedAction}</td>
                  </tr>
                ))}
                {filtered.length === 0 && (
                  <tr><td colSpan={6} className="p-6 text-gray-500 text-center">No courses match filters</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      )}

      <p className="text-center text-xs text-gray-400 mt-8">v2.2.0</p>
    </div>
  );
}

function Card({ label, value, color }: { label: string; value: string | number; color?: string }) {
  const c = color === "red" ? "bg-red-50 border-red-200 text-red-700"
    : color === "green" ? "bg-green-50 border-green-200 text-green-700"
    : "bg-white border-gray-200 text-gray-900";
  return (
    <div className={`rounded-lg border p-4 ${c}`}>
      <p className="text-xs text-gray-500 mb-1">{label}</p>
      <p className="text-xl font-bold">{value}</p>
    </div>
  );
}
