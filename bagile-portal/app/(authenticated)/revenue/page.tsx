"use client";

import React, { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { RevenueSummary, getRevenueSummary, formatCurrency } from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { SkeletonCard, SkeletonRow } from "@/components/ui/Skeleton";
import { BarChart, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, Legend } from "recharts";
import { TrendingUp, DollarSign, ShoppingCart, ChevronRight, ChevronDown } from "lucide-react";
import Link from "next/link";

const COUNTRY_NAMES: Record<string, string> = {
  GB: "United Kingdom", CZ: "Czech Republic", SI: "Slovenia", DE: "Germany",
  DK: "Denmark", MT: "Malta", PL: "Poland", IS: "Iceland", BE: "Belgium",
  SK: "Slovakia", FR: "France", BG: "Bulgaria", UA: "Ukraine", LT: "Lithuania",
  ZA: "South Africa", IT: "Italy", IN: "India", IE: "Ireland", CH: "Switzerland",
  NL: "Netherlands", ES: "Spain", SE: "Sweden", NO: "Norway", AT: "Austria",
  PT: "Portugal", FI: "Finland", HU: "Hungary", RO: "Romania", HR: "Croatia",
  GR: "Greece", EE: "Estonia", LV: "Latvia", LU: "Luxembourg", CY: "Cyprus",
  US: "United States", CA: "Canada", AU: "Australia", NZ: "New Zealand",
  BR: "Brazil", JP: "Japan", KR: "South Korea", SG: "Singapore", AE: "UAE",
  SA: "Saudi Arabia", IL: "Israel", NG: "Nigeria", KE: "Kenya", EG: "Egypt",
};

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

          {/* By Course Type — clickable rows */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
            <div className="px-5 py-3 border-b border-gray-200 flex items-center justify-between">
              <h2 className="text-sm font-semibold text-gray-900">By Course Type</h2>
              <span className="text-xs text-gray-400">Click to view courses</span>
            </div>
            <table className="w-full text-sm">
              <thead className="bg-gray-50">
                <tr>
                  <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Type</th>
                  <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Revenue</th>
                  <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Attendees</th>
                  <th className="w-8"></th>
                </tr>
              </thead>
              <tbody>
                {data.byCourseType.map((ct) => {
                  const pct = data.currentYearRevenue > 0
                    ? Math.round((ct.revenue / data.currentYearRevenue) * 100) : 0;
                  return (
                    <tr key={ct.courseType}
                      className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors"
                      onClick={() => window.location.href = `/courses?type=${ct.courseType}`}>
                      <td className="px-4 py-2.5">
                        <span className="font-medium text-gray-900">{ct.courseType}</span>
                        <span className="ml-2 text-xs text-gray-400">{pct}%</span>
                      </td>
                      <td className="px-4 py-2.5 text-right text-gray-700">{formatCurrency(ct.revenue)}</td>
                      <td className="px-4 py-2.5 text-right text-gray-500">{ct.attendeeCount}</td>
                      <td className="px-2 py-2.5 text-gray-300"><ChevronRight className="w-4 h-4" /></td>
                    </tr>
                  );
                })}
                {/* Total row */}
                <tr className="border-t-2 border-gray-200 bg-gray-50">
                  <td className="px-4 py-2.5 font-semibold text-gray-900">Total</td>
                  <td className="px-4 py-2.5 text-right font-semibold text-gray-900">
                    {formatCurrency(data.byCourseType.reduce((s, ct) => s + ct.revenue, 0))}
                  </td>
                  <td className="px-4 py-2.5 text-right font-semibold text-gray-700">
                    {data.byCourseType.reduce((s, ct) => s + ct.attendeeCount, 0)}
                  </td>
                  <td></td>
                </tr>
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

          {/* By Country/Region — grouped + collapsible */}
          {data.byCountry && data.byCountry.length > 0 && (
            <CountryBreakdown countries={data.byCountry} />
          )}
        </div>
      )}
    </>
  );
}

// ── Grouped Country Breakdown Component ──────────────────

function CountryBreakdown({ countries }: { countries: import("@/lib/api").CountryRevenue[] }) {
  const [expanded, setExpanded] = useState<Record<string, boolean>>({ UK: true, Europe: true });

  // Group by region
  const regions = countries.reduce((acc, c) => {
    const region = c.region || "Unknown";
    if (!acc[region]) acc[region] = [];
    acc[region].push(c);
    return acc;
  }, {} as Record<string, typeof countries>);

  // Region order: UK first, then Europe, then Rest of World, then Unknown
  const regionOrder = ["UK", "Europe", "Rest of World", "Unknown"];
  const sortedRegions = regionOrder.filter((r) => regions[r]);

  const regionTotals = (items: typeof countries) => ({
    revenue: items.reduce((s, c) => s + c.revenue, 0),
    attendees: items.reduce((s, c) => s + c.attendeeCount, 0),
  });

  const grandTotal = regionTotals(countries);

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
      <div className="px-5 py-3 border-b border-gray-200 flex items-center justify-between">
        <h2 className="text-sm font-semibold text-gray-900">By Region & Country</h2>
        <span className="text-xs text-gray-400">Click region to expand</span>
      </div>
      <table className="w-full text-sm">
        <thead className="bg-gray-50">
          <tr>
            <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Region / Country</th>
            <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Revenue</th>
            <th className="text-right px-4 py-2 text-xs font-medium text-gray-500 uppercase">Students</th>
          </tr>
        </thead>
        <tbody>
          {sortedRegions.map((region) => {
            const items = regions[region];
            const totals = regionTotals(items);
            const isOpen = expanded[region] ?? false;
            return (
              <React.Fragment key={region}>
                {/* Region header row */}
                <tr
                  className="border-t border-gray-200 bg-gray-50 hover:bg-gray-100 cursor-pointer"
                  onClick={() => setExpanded((prev) => ({ ...prev, [region]: !isOpen }))}
                >
                  <td className="px-4 py-2.5 font-semibold text-gray-900 flex items-center gap-2">
                    {isOpen ? <ChevronDown className="w-4 h-4 text-gray-400" /> : <ChevronRight className="w-4 h-4 text-gray-400" />}
                    {region}
                    <span className="text-xs text-gray-400 font-normal">({items.length} {items.length === 1 ? "country" : "countries"})</span>
                  </td>
                  <td className="px-4 py-2.5 text-right font-semibold text-gray-900">{formatCurrency(totals.revenue)}</td>
                  <td className="px-4 py-2.5 text-right font-semibold text-gray-700">{totals.attendees}</td>
                </tr>
                {/* Country rows (collapsed by default except UK + Europe) */}
                {isOpen && items.map((c) => (
                  <tr key={c.country} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-2 pl-10 text-gray-700">
                      {COUNTRY_NAMES[c.country] || c.country}
                      <span className="ml-1.5 text-xs text-gray-400">{c.country}</span>
                    </td>
                    <td className="px-4 py-2 text-right text-gray-700">{formatCurrency(c.revenue)}</td>
                    <td className="px-4 py-2 text-right text-gray-500">{c.attendeeCount}</td>
                  </tr>
                ))}
              </React.Fragment>
            );
          })}
          {/* Grand total */}
          <tr className="border-t-2 border-gray-200 bg-gray-50">
            <td className="px-4 py-2.5 font-semibold text-gray-900">Total</td>
            <td className="px-4 py-2.5 text-right font-semibold text-gray-900">{formatCurrency(grandTotal.revenue)}</td>
            <td className="px-4 py-2.5 text-right font-semibold text-gray-700">{grandTotal.attendees}</td>
          </tr>
        </tbody>
      </table>
    </div>
  );
}
