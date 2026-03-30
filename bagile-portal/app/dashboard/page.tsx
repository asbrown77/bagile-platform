"use client";

import { useEffect, useState } from "react";
import { MonitoringCourse, getMonitoring, getOrders } from "@/lib/api";

const API_KEY = process.env.NEXT_PUBLIC_BAGILE_API_KEY || "";

export default function Dashboard() {
  const [courses, setCourses] = useState<MonitoringCourse[]>([]);
  const [orderCount, setOrderCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

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
      const [monitoring, orders] = await Promise.all([
        getMonitoring(API_KEY, 60),
        getOrders(API_KEY, { status: "completed", pageSize: 1 }),
      ]);
      setCourses(monitoring);
      setOrderCount(orders.totalCount);
    } catch {
      setError("Failed to load data");
    } finally {
      setLoading(false);
    }
  }

  const atRisk = courses.filter((c) => c.monitoringStatus !== "healthy" && c.monitoringStatus !== "cancelled");
  const upcoming = courses.filter((c) => c.monitoringStatus !== "cancelled");

  return (
    <div className="max-w-6xl mx-auto py-8 px-4">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">BAgile</h1>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>}

      {loading ? (
        <p className="text-gray-500">Loading...</p>
      ) : (
        <>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
            <Card label="Upcoming Courses" value={upcoming.length} />
            <Card label="At Risk" value={atRisk.length} color={atRisk.length > 0 ? "red" : "green"} />
            <Card label="Completed Orders" value={orderCount} />
            <Card label="This Month" value={upcoming.filter((c) => new Date(c.startDate).getMonth() === new Date().getMonth()).length} />
          </div>

          {atRisk.length > 0 && (
            <div className="mb-8">
              <h2 className="font-semibold text-red-700 mb-3">Needs Attention</h2>
              <div className="space-y-2">
                {atRisk.map((c) => (
                  <a key={c.id} href={`/course/${c.id}`} className="block bg-red-50 border border-red-200 rounded-lg p-4 hover:bg-red-100">
                    <div className="flex justify-between items-center">
                      <div>
                        <p className="font-medium text-gray-900">{c.title}</p>
                        <p className="text-sm text-gray-600">
                          {new Date(c.startDate).toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" })}
                          {c.trainerName && ` — ${c.trainerName}`}
                        </p>
                      </div>
                      <div className="text-right">
                        <p className="text-lg font-bold text-red-700">{c.currentEnrolmentCount}/{c.minimumRequired}</p>
                        <p className="text-xs text-red-600">{c.recommendedAction}</p>
                      </div>
                    </div>
                  </a>
                ))}
              </div>
            </div>
          )}

          <h2 className="font-semibold text-gray-900 mb-3">Upcoming Courses</h2>
          <div className="bg-white rounded-lg shadow-sm border overflow-hidden">
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-left px-4 py-2 font-medium text-gray-600">Course</th>
                  <th className="text-left px-4 py-2 font-medium text-gray-600">Date</th>
                  <th className="text-left px-4 py-2 font-medium text-gray-600">Trainer</th>
                  <th className="text-center px-4 py-2 font-medium text-gray-600">Enrolled</th>
                  <th className="text-center px-4 py-2 font-medium text-gray-600">Status</th>
                </tr>
              </thead>
              <tbody>
                {upcoming.map((c) => (
                  <tr key={c.id} className="border-t hover:bg-gray-50 cursor-pointer" onClick={() => window.location.href = `/course/${c.id}`}>
                    <td className="px-4 py-3">
                      <p className="font-medium text-gray-900">{c.title}</p>
                      <p className="text-xs text-gray-500">{c.courseCode}</p>
                    </td>
                    <td className="px-4 py-3 text-gray-600">
                      {new Date(c.startDate).toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric" })}
                    </td>
                    <td className="px-4 py-3 text-gray-600">{c.trainerName || "—"}</td>
                    <td className="px-4 py-3 text-center">
                      <span className={`font-bold ${c.currentEnrolmentCount >= c.minimumRequired ? "text-green-600" : c.currentEnrolmentCount > 0 ? "text-amber-600" : "text-red-600"}`}>
                        {c.currentEnrolmentCount}
                      </span>
                      <span className="text-gray-400">/{c.minimumRequired}</span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      <span className={`px-2 py-1 rounded-full text-xs font-medium ${
                        c.monitoringStatus === "healthy" ? "bg-green-100 text-green-700" :
                        c.monitoringStatus === "warning" ? "bg-amber-100 text-amber-700" :
                        "bg-red-100 text-red-700"
                      }`}>
                        {c.monitoringStatus}
                      </span>
                    </td>
                  </tr>
                ))}
                {upcoming.length === 0 && (
                  <tr><td colSpan={5} className="p-6 text-gray-500 text-center">No upcoming courses</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </>
      )}

      <p className="text-center text-xs text-gray-400 mt-8">v2.1.0</p>
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
