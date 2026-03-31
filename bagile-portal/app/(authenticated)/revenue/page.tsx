"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { RevenueSummary, getRevenueSummary, formatCurrency } from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { SkeletonCard, SkeletonRow } from "@/components/ui/Skeleton";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Legend } from "recharts";
import { TrendingUp, DollarSign, ShoppingCart } from "lucide-react";
import Link from "next/link";

export default function RevenuePage() {
  const apiKey = useApiKey();
  const [data, setData] = useState<RevenueSummary | null>(null);
  const [year, setYear] = useState(new Date().getFullYear());
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!apiKey) return;
    setLoading(true);
    getRevenueSummary(apiKey, year)
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey, year]);

  // Fair YTD comparison: compare same months only
  const previousYtd = data?.previousYearYtdRevenue ?? 0;
  const yoyChange = data && previousYtd > 0
    ? Math.round(((data.currentYearRevenue - previousYtd) / previousYtd) * 100)
    : null;

  // Chart data: merge current + previous year monthly
  const chartData = data ? Array.from({ length: 12 }, (_, i) => {
    const month = i + 1;
    const current = data.monthlyBreakdown.find((m) => m.month === month);
    const prev = data.previousYearMonthly.find((m) => m.month === month);
    const monthNames = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];
    return {
      month: monthNames[i],
      monthNum: month,
      current: current?.revenue || 0,
      previous: prev?.revenue || 0,
      orders: current?.orderCount || 0,
      attendees: current?.attendeeCount || 0,
    };
  }) : [];

  return (
    <>
      <PageHeader
        title="Revenue"
        actions={
          <select value={year} onChange={(e) => setYear(Number(e.target.value))}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
            {[2026, 2025, 2024].map((y) => <option key={y} value={y}>{y}</option>)}
          </select>
        }
      />

      {/* KPI Cards */}
      {loading ? (
        <div className="grid grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
          {[1, 2, 3].map((i) => <SkeletonCard key={i} />)}
        </div>
      ) : data && (
        <div className="grid grid-cols-2 lg:grid-cols-3 gap-4 mb-8">
          <Card
            label={`Revenue ${year}`}
            value={formatCurrency(data.currentYearRevenue)}
            trend={yoyChange !== null ? { value: yoyChange, isPositive: yoyChange >= 0 } : undefined}
            subtitle={`vs ${formatCurrency(previousYtd)} same period ${year - 1}`}
            icon={<TrendingUp className="w-4 h-4" />}
          />
          <Card
            label="This Month"
            value={formatCurrency(data.currentMonthRevenue)}
            subtitle={`${data.currentMonthOrders} orders`}
            icon={<DollarSign className="w-4 h-4" />}
          />
          <Card
            label={`Orders ${year}`}
            value={data.currentYearOrders}
            subtitle="total orders"
            icon={<ShoppingCart className="w-4 h-4" />}
          />
        </div>
      )}

      {/* Monthly chart */}
      {!loading && chartData.length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-5 mb-8">
          <h2 className="text-sm font-semibold text-gray-900 mb-4">Monthly Revenue</h2>
          <ResponsiveContainer width="100%" height={300}>
            <BarChart data={chartData}>
              <XAxis dataKey="month" tick={{ fontSize: 12, fill: "#6b7280" }} />
              <YAxis tick={{ fontSize: 12, fill: "#6b7280" }} tickFormatter={(v) => `£${(v / 1000).toFixed(0)}k`} />
              <Tooltip
                formatter={(value) => formatCurrency(Number(value))}
                labelFormatter={(label) => `${label} ${year}`}
              />
              <Legend />
              <Bar dataKey="current" name={String(year)} fill="#003366" radius={[4, 4, 0, 0]} />
              <Bar dataKey="previous" name={String(year - 1)} fill="#dbeafe" radius={[4, 4, 0, 0]} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Monthly breakdown table */}
      {!loading && data && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
          {/* Monthly table */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            <div className="px-5 py-3 border-b border-gray-200">
              <h2 className="text-sm font-semibold text-gray-900">Monthly Breakdown</h2>
            </div>
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Month</th>
                  <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Revenue</th>
                  <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Orders</th>
                </tr>
              </thead>
              <tbody>
                {data.monthlyBreakdown.map((m) => (
                  <tr key={m.month} className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer"
                    onClick={() => window.location.href = `/revenue/${m.year}/${m.month}`}>
                    <td className="px-4 py-2.5 font-medium text-gray-900">{m.monthName} {m.year}</td>
                    <td className="px-4 py-2.5 text-right text-gray-700">{formatCurrency(m.revenue)}</td>
                    <td className="px-4 py-2.5 text-right text-gray-500">{m.orderCount}</td>
                  </tr>
                ))}
                {data.monthlyBreakdown.length === 0 && (
                  <tr><td colSpan={3} className="px-4 py-6 text-center text-gray-400">No revenue data for {year}</td></tr>
                )}
              </tbody>
            </table>
          </div>

          {/* By Course Type */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            <div className="px-5 py-3 border-b border-gray-200">
              <h2 className="text-sm font-semibold text-gray-900">By Course Type</h2>
            </div>
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Type</th>
                  <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Revenue</th>
                  <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Attendees</th>
                </tr>
              </thead>
              <tbody>
                {data.byCourseType.map((ct) => (
                  <tr key={ct.courseType} className="border-t border-gray-100">
                    <td className="px-4 py-2.5 font-medium text-gray-900">{ct.courseType}</td>
                    <td className="px-4 py-2.5 text-right text-gray-700">{formatCurrency(ct.revenue)}</td>
                    <td className="px-4 py-2.5 text-right text-gray-500">{ct.attendeeCount}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* By Source + By Country side by side */}
      {!loading && data && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {/* By Source */}
          {data.bySource.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="px-5 py-3 border-b border-gray-200">
                <h2 className="text-sm font-semibold text-gray-900">By Source</h2>
              </div>
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Source</th>
                    <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Revenue</th>
                    <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Attendees</th>
                  </tr>
                </thead>
                <tbody>
                  {data.bySource.map((s) => (
                    <tr key={s.source} className="border-t border-gray-100">
                      <td className="px-4 py-2.5 font-medium text-gray-900 capitalize">{s.source}</td>
                      <td className="px-4 py-2.5 text-right text-gray-700">{formatCurrency(s.revenue)}</td>
                      <td className="px-4 py-2.5 text-right text-gray-500">{s.attendeeCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {/* By Country/Region */}
          {data.byCountry && data.byCountry.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="px-5 py-3 border-b border-gray-200">
                <h2 className="text-sm font-semibold text-gray-900">By Region & Country</h2>
              </div>
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Region</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Country</th>
                    <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Revenue</th>
                    <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Students</th>
                  </tr>
                </thead>
                <tbody>
                  {data.byCountry.map((c) => (
                    <tr key={c.country} className="border-t border-gray-100">
                      <td className="px-4 py-2.5 text-gray-500">{c.region}</td>
                      <td className="px-4 py-2.5 font-medium text-gray-900">{c.country}</td>
                      <td className="px-4 py-2.5 text-right text-gray-700">{formatCurrency(c.revenue)}</td>
                      <td className="px-4 py-2.5 text-right text-gray-500">{c.attendeeCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </>
  );
}
