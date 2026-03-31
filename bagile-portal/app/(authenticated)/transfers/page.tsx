"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { PendingTransfer, getPendingTransfers, formatDate } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { Badge } from "@/components/ui/Badge";
import { ArrowLeftRight, CheckCircle } from "lucide-react";

export default function TransfersPage() {
  const apiKey = useApiKey();
  const [pending, setPending] = useState<PendingTransfer[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!apiKey) return;
    setError("");
    getPendingTransfers(apiKey)
      .then((data) => setPending(Array.isArray(data) ? data : []))
      .catch(() => setError("Failed to load transfers"))
      .finally(() => setLoading(false));
  }, [apiKey]);

  return (
    <>
      <PageHeader title="Transfers" subtitle="Attendees awaiting rebooking" />
      {error && <div className="mb-4"><AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner></div>}

      {/* Mobile cards */}
      <div className="md:hidden space-y-3">
        {!loading && pending.map((t, i) => (
          <div key={i} className="bg-white rounded-xl border border-gray-200 shadow-sm p-4">
            <div className="flex items-start justify-between">
              <div>
                <p className="font-medium text-gray-900">{t.studentName}</p>
                <a href={`mailto:${t.studentEmail}`} className="text-xs text-brand-600">{t.studentEmail}</a>
              </div>
              <Badge variant="warning" dot>Pending</Badge>
            </div>
            <div className="mt-2 text-xs text-gray-500">
              <p>{t.courseTitle || t.courseCode}</p>
              {t.cancelledDate && <p>Cancelled: {formatDate(t.cancelledDate)}</p>}
            </div>
          </div>
        ))}
        {!loading && pending.length === 0 && (
          <EmptyState icon={<CheckCircle className="w-10 h-10" />} title="All clear" description="No attendees pending transfer" />
        )}
      </div>

      {/* Desktop table */}
      <div className="hidden md:block bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Student</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Email</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Original Course</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Cancelled</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={5} />)}
            {!loading && pending.map((t, i) => (
              <tr key={i} className="border-t border-gray-100 hover:bg-gray-50">
                <td className="px-4 py-3 font-medium text-gray-900">{t.studentName}</td>
                <td className="px-4 py-3"><a href={`mailto:${t.studentEmail}`} className="text-brand-600 hover:underline">{t.studentEmail}</a></td>
                <td className="px-4 py-3 text-gray-600">{t.courseTitle || t.courseCode}</td>
                <td className="px-4 py-3 text-gray-500">{formatDate(t.cancelledDate)}</td>
                <td className="px-4 py-3 text-center"><Badge variant="warning" dot>Pending</Badge></td>
              </tr>
            ))}
            {!loading && pending.length === 0 && (
              <tr><td colSpan={5}>
                <EmptyState icon={<CheckCircle className="w-10 h-10" />} title="All clear" description="No attendees pending transfer" />
              </td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
