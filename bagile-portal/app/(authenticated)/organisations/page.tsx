"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { OrganisationAnalytics, getOrganisationAnalytics, formatCurrency } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { Badge } from "@/components/ui/Badge";
import { Building2, Search, ArrowUpDown } from "lucide-react";

type SortKey = "totalSpend" | "orderCount" | "delegateCount" | "company";

export default function OrganisationsPage() {
  const apiKey = useApiKey();
  const [orgs, setOrgs] = useState<OrganisationAnalytics[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState<SortKey>("totalSpend");
  const [sortAsc, setSortAsc] = useState(false);
  const [yearFilter, setYearFilter] = useState<string>(String(new Date().getFullYear()));

  useEffect(() => {
    if (!apiKey) return;
    setLoading(true);
    const yearNum = yearFilter === "all" ? undefined : Number(yearFilter);
    getOrganisationAnalytics(apiKey, yearNum)
      .then((data) => setOrgs(data.organisations || []))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey, yearFilter]);

  const filtered = orgs
    .filter((o) => !search || o.company.toLowerCase().includes(search.toLowerCase()))
    .sort((a, b) => {
      const av = sortBy === "company" ? a.company.toLowerCase() : (a[sortBy] ?? 0);
      const bv = sortBy === "company" ? b.company.toLowerCase() : (b[sortBy] ?? 0);
      if (av < bv) return sortAsc ? -1 : 1;
      if (av > bv) return sortAsc ? 1 : -1;
      return 0;
    });

  function toggleSort(key: SortKey) {
    if (sortBy === key) setSortAsc(!sortAsc);
    else { setSortBy(key); setSortAsc(false); }
  }

  function SortHeader({ label, sortKey }: { label: string; sortKey: SortKey }) {
    return (
      <button onClick={() => toggleSort(sortKey)} className="flex items-center gap-1 text-xs font-medium text-gray-500 uppercase tracking-wide hover:text-gray-700">
        {label}
        <ArrowUpDown className={`w-3 h-3 ${sortBy === sortKey ? "text-brand-600" : "text-gray-300"}`} />
      </button>
    );
  }

  return (
    <>
      <PageHeader title="Organisations" subtitle={`Companies by bookings and spend${yearFilter === "all" ? " — all time" : ` — ${yearFilter}`}`}
        actions={
          <div className="inline-flex rounded-lg border border-gray-300 overflow-hidden">
            {["all", "2026", "2025", "2024"].map((opt) => (
              <button
                key={opt}
                onClick={() => setYearFilter(opt)}
                className={`px-3 py-1.5 text-xs font-medium transition-colors ${
                  yearFilter === opt ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"
                } ${opt !== "all" ? "border-l border-gray-300" : ""}`}
              >
                {opt === "all" ? "All time" : opt}
              </button>
            ))}
          </div>
        }
      />

      {/* Search */}
      <div className="relative mb-4">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
        <input
          type="text"
          placeholder="Search company name..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
        />
      </div>

      {/* Mobile cards */}
      <div className="md:hidden space-y-3">
        {!loading && filtered.map((o) => (
          <div key={o.company}
            onClick={() => window.location.href = `/organisations/${encodeURIComponent(o.company)}`}
            className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 cursor-pointer hover:bg-gray-50">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-medium text-gray-900">{o.company}</p>
                {o.partnerType === "ptn" && <Badge variant="info" dot>Partner</Badge>}
              </div>
              <p className="font-bold text-gray-900">{formatCurrency(o.totalSpend)}</p>
            </div>
            <div className="flex gap-4 mt-2 text-xs text-gray-500">
              <span>{o.orderCount} orders</span>
              <span>{o.delegateCount} delegates</span>
            </div>
          </div>
        ))}
        {!loading && filtered.length === 0 && (
          <EmptyState icon={<Building2 className="w-10 h-10" />}
            title={search ? "No matches" : "No data"}
            description={search ? `No companies matching "${search}"` : "No organisation data found"} />
        )}
      </div>

      {/* Desktop table */}
      <div className="hidden md:block bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3"><SortHeader label="Company" sortKey="company" /></th>
              <th className="text-center px-4 py-3"><SortHeader label="Orders" sortKey="orderCount" /></th>
              <th className="text-center px-4 py-3"><SortHeader label="Delegates" sortKey="delegateCount" /></th>
              <th className="text-right px-4 py-3"><SortHeader label="Spend" sortKey="totalSpend" /></th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Type</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 8 }).map((_, i) => <SkeletonRow key={i} cols={5} />)}
            {!loading && filtered.map((o) => (
              <tr key={o.company} className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer"
                onClick={() => window.location.href = `/organisations/${encodeURIComponent(o.company)}`}>
                <td className="px-4 py-3 font-medium text-gray-900">{o.company}</td>
                <td className="px-4 py-3 text-center text-gray-700">{o.orderCount}</td>
                <td className="px-4 py-3 text-center text-gray-700">{o.delegateCount}</td>
                <td className="px-4 py-3 text-right font-medium text-gray-900">{formatCurrency(o.totalSpend)}</td>
                <td className="px-4 py-3 text-center">
                  {o.partnerType === "ptn" ? <Badge variant="info" dot>Partner</Badge> : null}
                </td>
              </tr>
            ))}
            {!loading && filtered.length === 0 && (
              <tr><td colSpan={5}>
                <EmptyState icon={<Building2 className="w-10 h-10" />}
                  title={search ? "No matches" : "No data"}
                  description={search ? `No companies matching "${search}"` : "No organisation booking data found"} />
              </td></tr>
            )}
          </tbody>
        </table>
        {!loading && filtered.length > 0 && (
          <div className="px-4 py-2 bg-gray-50 border-t border-gray-200 text-xs text-gray-500">
            {filtered.length} organisation{filtered.length !== 1 ? "s" : ""}
            {search && ` matching "${search}"`}
          </div>
        )}
      </div>
    </>
  );
}
