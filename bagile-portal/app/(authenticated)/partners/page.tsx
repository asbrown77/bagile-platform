"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { PartnerAnalytics, getPartnerAnalytics, formatCurrency } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { Badge } from "@/components/ui/Badge";
import { Handshake, AlertTriangle } from "lucide-react";

export default function PartnersPage() {
  const apiKey = useApiKey();
  const [partners, setPartners] = useState<PartnerAnalytics[]>([]);
  const [loading, setLoading] = useState(true);
  const [year, setYear] = useState(new Date().getFullYear());

  useEffect(() => {
    if (!apiKey) return;
    setLoading(true);
    getPartnerAnalytics(apiKey, year)
      .then(setPartners)
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey, year]);

  const mismatches = partners.filter((p) => p.tierMismatch);

  return (
    <>
      <PageHeader title="Partners" subtitle={`PTN partner tier tracking — ${year}`}
        actions={
          <select value={year} onChange={(e) => setYear(Number(e.target.value))}
            className="border border-gray-300 rounded-lg px-3 py-1.5 text-sm bg-white">
            {[2026, 2025, 2024].map((y) => <option key={y} value={y}>{y}</option>)}
          </select>
        }
      />

      {mismatches.length > 0 && (
        <div className="bg-amber-50 border border-amber-200 rounded-xl p-4 mb-6 flex items-center gap-3">
          <AlertTriangle className="w-5 h-5 text-amber-600 shrink-0" />
          <div>
            <p className="text-sm font-medium text-amber-800">
              {mismatches.length} partner{mismatches.length !== 1 ? "s" : ""} billed at wrong rate
            </p>
            <p className="text-xs text-amber-600 mt-0.5">
              {mismatches.map((p) => p.name).join(", ")}
            </p>
          </div>
        </div>
      )}

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Partner</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Current Tier</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Calculated</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Bookings</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Delegates</th>
              <th className="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Spend</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={6} />)}
            {!loading && partners.map((p) => (
              <tr key={p.name} className={`border-t border-gray-100 hover:bg-gray-50 ${p.tierMismatch ? "bg-amber-50/50" : ""}`}>
                <td className="px-4 py-3">
                  <div className="flex items-center gap-2">
                    <p className="font-medium text-gray-900">{p.name}</p>
                    {p.tierMismatch && <AlertTriangle className="w-3.5 h-3.5 text-amber-500" />}
                  </div>
                  {p.contactEmail && <p className="text-xs text-gray-400">{p.contactEmail}</p>}
                </td>
                <td className="px-4 py-3 text-center">
                  <Badge variant="info">{p.ptnTier || "—"}</Badge>
                  {p.discountRate ? <p className="text-xs text-gray-400 mt-0.5">{p.discountRate}%</p> : null}
                </td>
                <td className="px-4 py-3 text-center">
                  <Badge variant={p.tierMismatch ? "warning" : "success"}>{p.calculatedTier}</Badge>
                  <p className="text-xs text-gray-400 mt-0.5">{p.calculatedDiscount}%</p>
                </td>
                <td className="px-4 py-3 text-center text-gray-700 hidden md:table-cell">{p.bookingsThisYear}</td>
                <td className="px-4 py-3 text-center text-gray-700 hidden md:table-cell">{p.delegatesThisYear}</td>
                <td className="px-4 py-3 text-right font-medium text-gray-900">{formatCurrency(p.spendThisYear)}</td>
              </tr>
            ))}
            {!loading && partners.length === 0 && (
              <tr><td colSpan={6}>
                <EmptyState icon={<Handshake className="w-10 h-10" />} title="No partners" description={`No PTN partners with data for ${year}`} />
              </td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
