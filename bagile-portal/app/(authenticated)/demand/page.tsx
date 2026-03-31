"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { CourseDemandResult, getCourseDemand } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Legend } from "recharts";
import { TrendingUp } from "lucide-react";

export default function DemandPage() {
  const apiKey = useApiKey();
  const [data, setData] = useState<CourseDemandResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [months, setMonths] = useState(12);

  useEffect(() => {
    if (!apiKey) return;
    setLoading(true);
    setError("");
    getCourseDemand(apiKey, months)
      .then(setData)
      .catch(() => setError("Failed to load demand data"))
      .finally(() => setLoading(false));
  }, [apiKey, months]);

  // Build chart data from monthly trend — pivot by course type
  const chartData = data ? (() => {
    const monthMap = new Map<string, Record<string, number>>();
    for (const t of data.monthlyTrend) {
      const key = `${t.year}-${String(t.month).padStart(2, "0")}`;
      if (!monthMap.has(key)) monthMap.set(key, {});
      monthMap.get(key)![t.courseType] = t.enrolments;
    }
    return [...monthMap.entries()]
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([key, types]) => ({
        month: key.slice(5) + "/" + key.slice(2, 4),
        ...types,
      }));
  })() : [];

  const courseTypeColors: Record<string, string> = {
    PSM: "#003366", PSPO: "#0059b3", PSPOAI: "#2563eb", PSPOA: "#3b82f6",
    PAL: "#059669", PALE: "#10b981", PSK: "#8b5cf6", PSMA: "#d97706",
    PSMAI: "#f59e0b", PSFS: "#ec4899", APS: "#6366f1", EBM: "#14b8a6",
  };

  const allTypes = data ? data.courseTypes.map((ct) => ct.courseType) : [];

  return (
    <>
      <PageHeader
        title="Course Demand"
        subtitle={`Last ${months} months — which courses sell best`}
        actions={
          <div className="inline-flex rounded-lg border border-gray-300 overflow-hidden">
            {[6, 12, 24].map((m) => (
              <button key={m} onClick={() => setMonths(m)}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${
                  months === m ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"
                } ${m !== 6 ? "border-l border-gray-300" : ""}`}>
                {m}m
              </button>
            ))}
          </div>
        }
      />

      {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

      {/* Course type table */}
      {!loading && data && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            <div className="px-5 py-3 border-b border-gray-200">
              <h2 className="text-sm font-semibold text-gray-900">By Course Type</h2>
            </div>
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Type</th>
                  <th className="text-center px-4 py-2 text-xs font-medium text-gray-500 uppercase">Courses</th>
                  <th className="text-center px-4 py-2 text-xs font-medium text-gray-500 uppercase">Enrolments</th>
                  <th className="text-center px-4 py-2 text-xs font-medium text-gray-500 uppercase">Avg Att.</th>
                  <th className="text-center px-4 py-2 text-xs font-medium text-gray-500 uppercase">Fill %</th>
                </tr>
              </thead>
              <tbody>
                {data.courseTypes.map((ct) => (
                  <tr key={ct.courseType} className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer"
                    onClick={() => window.location.href = `/courses?type=${ct.courseType}`}>
                    <td className="px-4 py-2.5">
                      <div className="flex items-center gap-2">
                        <div className="w-3 h-3 rounded" style={{ backgroundColor: courseTypeColors[ct.courseType] || "#6b7280" }} />
                        <span className="font-medium text-gray-900">{ct.courseType}</span>
                      </div>
                    </td>
                    <td className="px-4 py-2.5 text-center text-gray-700">{ct.coursesRun}</td>
                    <td className="px-4 py-2.5 text-center text-gray-700">{ct.totalEnrolments}</td>
                    <td className="px-4 py-2.5 text-center text-gray-700">{ct.avgAttendees}</td>
                    <td className="px-4 py-2.5 text-center">
                      <span className={`font-medium ${ct.avgFillPct >= 100 ? "text-green-600" : ct.avgFillPct >= 60 ? "text-amber-600" : "text-red-600"}`}>
                        {ct.avgFillPct}%
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Trend chart */}
          {chartData.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5">
              <h2 className="text-sm font-semibold text-gray-900 mb-4">Monthly Enrolment Trend</h2>
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={chartData}>
                  <XAxis dataKey="month" tick={{ fontSize: 10, fill: "#6b7280" }} />
                  <YAxis tick={{ fontSize: 10, fill: "#6b7280" }} />
                  <Tooltip />
                  <Legend wrapperStyle={{ fontSize: 11 }} />
                  {allTypes.slice(0, 6).map((type) => (
                    <Bar key={type} dataKey={type} stackId="a" fill={courseTypeColors[type] || "#6b7280"} />
                  ))}
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
        </div>
      )}

      {loading && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <tbody>
              {Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={5} />)}
            </tbody>
          </table>
        </div>
      )}

      {!loading && (!data || data.courseTypes.length === 0) && (
        <EmptyState icon={<TrendingUp className="w-10 h-10" />} title="No demand data" description="No course data available for this period" />
      )}
    </>
  );
}
