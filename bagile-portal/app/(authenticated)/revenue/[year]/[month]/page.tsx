"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { MonthDrilldown, getRevenueMonthDrilldown, formatCurrency, formatDate } from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { Badge } from "@/components/ui/Badge";
import { SkeletonRow } from "@/components/ui/Skeleton";
import Link from "next/link";

export default function MonthDrilldownPage() {
  const apiKey = useApiKey();
  const params = useParams();
  const year = Number(params.year);
  const month = Number(params.month);
  const [data, setData] = useState<MonthDrilldown | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!apiKey || !year || !month) return;
    getRevenueMonthDrilldown(apiKey, year, month)
      .then(setData)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey, year, month]);

  return (
    <>
      <div className="mb-2">
        <Link href="/revenue" className="text-sm text-brand-600 hover:text-brand-700">&larr; Back to Revenue</Link>
      </div>
      <PageHeader title={data ? `${data.monthName} ${data.year}` : "Loading..."} />

      {data && (
        <div className="grid grid-cols-2 gap-4 mb-6">
          <Card label="Total Revenue" value={formatCurrency(data.totalRevenue)} />
          <Card label="Total Orders" value={data.totalOrders} />
        </div>
      )}

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Date</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Company</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Course</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Payment</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase">Attendees</th>
              <th className="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase">Revenue</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 8 }).map((_, i) => <SkeletonRow key={i} cols={6} />)}

            {data?.orders.map((o) => (
              <tr key={`${o.orderId}-${o.courseCode}`} className="border-t border-gray-100 hover:bg-gray-50">
                <td className="px-4 py-2.5 text-gray-600">{formatDate(o.orderDate)}</td>
                <td className="px-4 py-2.5">
                  <p className="font-medium text-gray-900">{o.company || o.contactName || "—"}</p>
                  <p className="text-xs text-gray-400">#{o.externalId}</p>
                </td>
                <td className="px-4 py-2.5 hidden md:table-cell">
                  <p className="text-gray-700">{o.courseName || "—"}</p>
                  <p className="text-xs text-gray-400 font-mono">{o.courseCode}</p>
                </td>
                <td className="px-4 py-2.5 hidden lg:table-cell text-gray-600">{o.paymentMethod || "—"}</td>
                <td className="px-4 py-2.5 text-center text-gray-700">{o.attendeeCount}</td>
                <td className="px-4 py-2.5 text-right">
                  <span className="font-medium text-gray-900">{formatCurrency(o.netRevenue)}</span>
                  {o.refundAmount > 0 && (
                    <p className="text-xs text-red-500">-{formatCurrency(o.refundAmount)} refunded</p>
                  )}
                </td>
              </tr>
            ))}

            {!loading && (!data || data.orders.length === 0) && (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-gray-400">No orders for this month</td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
