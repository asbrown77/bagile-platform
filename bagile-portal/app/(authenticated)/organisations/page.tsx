"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { OrganisationAnalytics, getOrganisationAnalytics, formatCurrency } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { Building2 } from "lucide-react";

export default function OrganisationsPage() {
  const apiKey = useApiKey();
  const [orgs, setOrgs] = useState<OrganisationAnalytics[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!apiKey) return;
    getOrganisationAnalytics(apiKey)
      .then((data) => setOrgs(data.organisations || []))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey]);

  return (
    <>
      <PageHeader title="Organisations" subtitle="Companies by bookings and spend this year" />
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Company</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Orders</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Delegates</th>
              <th className="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Spend</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 6 }).map((_, i) => <SkeletonRow key={i} cols={4} />)}
            {!loading && orgs.map((o) => (
              <tr key={o.company} className="border-t border-gray-100 hover:bg-gray-50">
                <td className="px-4 py-3 font-medium text-gray-900">{o.company}</td>
                <td className="px-4 py-3 text-center text-gray-700">{o.orderCount}</td>
                <td className="px-4 py-3 text-center text-gray-700">{o.delegateCount}</td>
                <td className="px-4 py-3 text-right font-medium text-gray-900">{formatCurrency(o.totalSpend)}</td>
              </tr>
            ))}
            {!loading && orgs.length === 0 && (
              <tr><td colSpan={4}>
                <EmptyState icon={<Building2 className="w-10 h-10" />} title="No data" description="No organisation booking data found" />
              </td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
