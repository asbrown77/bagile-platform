"use client";

import { useEffect, useState } from "react";
import { MonitoringCourse, getMonitoring, getOrders } from "@/lib/api";

export default function Dashboard() {
  const [courses, setCourses] = useState<MonitoringCourse[]>([]);
  const [revenue, setRevenue] = useState<{ month: number; year: number }>({ month: 0, year: 0 });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [apiKey, setApiKey] = useState("");

  useEffect(() => {
    const key = localStorage.getItem("bagile_api_key");
    if (key) {
      setApiKey(key);
      loadData(key);
    } else {
      setLoading(false);
    }
  }, []);

  async function loadData(key: string) {
    setLoading(true);
    setError("");
    try {
      const monitoring = await getMonitoring(key, 60);
      setCourses(monitoring);

      const now = new Date();
      const monthStart = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, "0")}-01`;
      const yearStart = `${now.getFullYear()}-01-01`;

      const [monthOrders, yearOrders] = await Promise.all([
        getOrders(key, { status: "completed", from: monthStart, pageSize: 1 }),
        getOrders(key, { status: "completed", from: yearStart, pageSize: 1 }),
      ]);

      setRevenue({
        month: monthOrders.items.reduce((sum, o) => sum + o.totalAmount, 0),
        year: yearOrders.items.reduce((sum, o) => sum + o.totalAmount, 0),
      });
    } catch {
      setError("Failed to load data. Check your API key.");
    } finally {
      setLoading(false);
    }
  }

  function handleKeySubmit(e: React.FormEvent) {
    e.preventDefault();
    localStorage.setItem("bagile_api_key", apiKey);
    loadData(apiKey);
  }

  const atRisk = courses.filter((c) => c.needsAttention && c.status !== "cancelled");
  const upcoming = courses.filter((c) => c.status !== "cancelled");

  if (!loading && !apiKey) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <form onSubmit={handleKeySubmit} className="bg-white rounded-lg shadow-md p-8 max-w-md w-full">
          <h1 className="text-2xl font-bold text-gray-900 mb-2">BAgile Dashboard</h1>
          <p className="text-gray-600 text-sm mb-4">
            Enter your API key to access the dashboard.
            Get one at <a href="/" className="text-blue-600 underline">portal.bagile.co.uk</a>
          </p>
          <input
            type="password"
            placeholder="Paste your API key"
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            className="w-full border rounded px-3 py-2 text-sm mb-3"
          />
          <button type="submit" className="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700 text-sm">
            Connect
          </button>
        </form>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">BAgile Dashboard</h1>
          <p className="text-gray-500 text-sm">Live data from the platform</p>
        </div>
        <div className="flex gap-3">
          <a href="/" className="text-sm text-gray-500 hover:text-gray-700">API Keys</a>
          <button
            onClick={() => { localStorage.removeItem("bagile_api_key"); setApiKey(""); setCourses([]); }}
            className="text-sm text-gray-500 hover:text-gray-700"
          >
            Disconnect
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>
      )}

      {loading ? (
        <p className="text-gray-500">Loading...</p>
      ) : (
        <>
          {/* Summary cards */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
            <Card label="Upcoming Courses" value={upcoming.length} />
            <Card label="At Risk" value={atRisk.length} color={atRisk.length > 0 ? "red" : "green"} />
            <Card label="Revenue This Month" value={`£${revenue.month.toLocaleString()}`} />
            <Card label="Revenue This Year" value={`£${revenue.year.toLocaleString()}`} />
          </div>

          {/* At risk courses */}
          {atRisk.length > 0 && (
            <div className="mb-8">
              <h2 className="font-semibold text-red-700 mb-3">Needs Attention</h2>
              <div className="space-y-2">
                {atRisk.map((c) => (
                  <a key={c.id} href={`/course/${c.id}`} className="block bg-red-50 border border-red-200 rounded-lg p-4 hover:bg-red-100">
                    <div className="flex justify-between items-center">
                      <div>
                        <p className="font-medium text-gray-900">{c.name}</p>
                        <p className="text-sm text-gray-600">
                          {new Date(c.startDate).toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" })}
                          {c.trainerName && ` — ${c.trainerName}`}
                        </p>
                      </div>
                      <div className="text-right">
                        <p className="text-lg font-bold text-red-700">{c.enrolledCount}/{c.minimumAttendees}</p>
                        <p className="text-xs text-red-600">{c.recommendedAction}</p>
                      </div>
                    </div>
                  </a>
                ))}
              </div>
            </div>
          )}

          {/* All upcoming courses */}
          <div>
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
                        <p className="font-medium text-gray-900">{c.name}</p>
                        <p className="text-xs text-gray-500">{c.courseCode}</p>
                      </td>
                      <td className="px-4 py-3 text-gray-600">
                        {new Date(c.startDate).toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric" })}
                      </td>
                      <td className="px-4 py-3 text-gray-600">{c.trainerName || "—"}</td>
                      <td className="px-4 py-3 text-center">
                        <span className={`font-bold ${c.enrolledCount >= c.minimumAttendees ? "text-green-600" : c.enrolledCount > 0 ? "text-amber-600" : "text-red-600"}`}>
                          {c.enrolledCount}
                        </span>
                        <span className="text-gray-400">/{c.minimumAttendees}</span>
                      </td>
                      <td className="px-4 py-3 text-center">
                        <StatusBadge status={c.guaranteedToRun ? "confirmed" : c.needsAttention ? "at-risk" : "on-track"} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              {upcoming.length === 0 && (
                <p className="p-6 text-gray-500 text-center">No upcoming courses. Data may still be importing.</p>
              )}
            </div>
          </div>
        </>
      )}

      <p className="text-center text-xs text-gray-400 mt-8">BAgile Dashboard v1.5.0</p>
    </div>
  );
}

function Card({ label, value, color }: { label: string; value: string | number; color?: string }) {
  const colors = {
    red: "bg-red-50 border-red-200 text-red-700",
    green: "bg-green-50 border-green-200 text-green-700",
    default: "bg-white border-gray-200 text-gray-900",
  };
  return (
    <div className={`rounded-lg border p-4 ${colors[color as keyof typeof colors] || colors.default}`}>
      <p className="text-xs text-gray-500 mb-1">{label}</p>
      <p className="text-xl font-bold">{value}</p>
    </div>
  );
}

function StatusBadge({ status }: { status: string }) {
  const styles: Record<string, string> = {
    confirmed: "bg-green-100 text-green-700",
    "on-track": "bg-blue-100 text-blue-700",
    "at-risk": "bg-red-100 text-red-700",
  };
  return (
    <span className={`px-2 py-1 rounded-full text-xs font-medium ${styles[status] || "bg-gray-100 text-gray-600"}`}>
      {status.replace("-", " ")}
    </span>
  );
}
