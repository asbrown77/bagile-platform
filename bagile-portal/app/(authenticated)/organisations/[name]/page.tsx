"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  OrganisationDetail, OrgCourseHistory,
  getOrganisationDetail, getOrganisationCourseHistory, formatCurrency, formatDate
} from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonCard, SkeletonRow } from "@/components/ui/Skeleton";
import { Badge } from "@/components/ui/Badge";
import { Building2, ShoppingCart, Users, TrendingUp, Calendar } from "lucide-react";
import Link from "next/link";

export default function OrganisationDetailPage() {
  const apiKey = useApiKey();
  const params = useParams();
  const orgName = decodeURIComponent(String(params.name));
  const [org, setOrg] = useState<OrganisationDetail | null>(null);
  const [history, setHistory] = useState<OrgCourseHistory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!apiKey || !orgName) return;
    Promise.all([
      getOrganisationDetail(apiKey, orgName).catch(() => null),
      getOrganisationCourseHistory(apiKey, orgName).catch(() => []),
    ])
      .then(([detail, courses]) => {
        setOrg(detail);
        setHistory(courses);
      })
      .catch(() => setError("Failed to load organisation"))
      .finally(() => setLoading(false));
  }, [apiKey, orgName]);

  const relationshipDays = org?.firstOrderDate
    ? Math.round((Date.now() - new Date(org.firstOrderDate).getTime()) / (1000 * 60 * 60 * 24))
    : 0;

  return (
    <>
      <div className="mb-2">
        <Link href="/organisations" className="text-sm text-brand-600 hover:text-brand-700">&larr; Organisations</Link>
      </div>

      <PageHeader
        title={orgName}
        subtitle={org?.primaryDomain || undefined}
      />

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded-lg mb-6 text-sm">{error}</div>}

      {/* KPI Cards */}
      {loading ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          {[1, 2, 3, 4].map((i) => <SkeletonCard key={i} />)}
        </div>
      ) : org ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
          <Card label="Total Revenue" value={formatCurrency(org.totalRevenue)} icon={<TrendingUp className="w-4 h-4" />} />
          <Card label="Orders" value={org.totalOrders} icon={<ShoppingCart className="w-4 h-4" />} />
          <Card label="Students" value={org.totalStudents} icon={<Users className="w-4 h-4" />} />
          <Card
            label="Relationship"
            value={relationshipDays > 365 ? `${Math.round(relationshipDays / 365)}y` : `${relationshipDays}d`}
            subtitle={org.firstOrderDate ? `Since ${formatDate(org.firstOrderDate)}` : "—"}
            icon={<Calendar className="w-4 h-4" />}
          />
        </div>
      ) : !loading && (
        <div className="mb-8">
          <AlertBanner variant="info">
            <strong>{orgName}</strong> appears in billing data but isn't registered as an organisation yet.
            Their booking data is available on the Organisations analytics page.
          </AlertBanner>
        </div>
      )}

      {/* Dates summary */}
      {org && (
        <div className="flex gap-6 mb-6 text-sm text-gray-500">
          {org.firstOrderDate && <span>First order: {formatDate(org.firstOrderDate)}</span>}
          {org.lastOrderDate && <span>Last order: {formatDate(org.lastOrderDate)}</span>}
          {org.lastCourseDate && <span>Last course: {formatDate(org.lastCourseDate)}</span>}
        </div>
      )}

      {/* Course History */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-5 py-3 border-b border-gray-200">
          <h2 className="text-sm font-semibold text-gray-900">Course History</h2>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Course</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Public</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Private</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Total</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Last Run</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={5} />)}
            {!loading && history.map((h) => (
              <tr key={h.courseCode} className="border-t border-gray-100 hover:bg-gray-50">
                <td className="px-4 py-3">
                  <p className="font-medium text-gray-900">{h.courseTitle || h.courseCode}</p>
                  <p className="text-xs text-gray-400 font-mono">{h.courseCode}</p>
                </td>
                <td className="px-4 py-3 text-center text-gray-700">{h.publicCount || "—"}</td>
                <td className="px-4 py-3 text-center text-gray-700">{h.privateCount || "—"}</td>
                <td className="px-4 py-3 text-center font-bold text-gray-900">{h.totalCount}</td>
                <td className="px-4 py-3 text-gray-500 hidden md:table-cell">{formatDate(h.lastRunDate)}</td>
              </tr>
            ))}
            {!loading && history.length === 0 && (
              <tr><td colSpan={5}>
                <EmptyState icon={<Building2 className="w-10 h-10" />} title="No course history" description="No courses found for this organisation" />
              </td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
