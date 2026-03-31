"use client";

import { useEffect, useState } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { PendingTransfer, getPendingTransfers, formatDate } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { Badge } from "@/components/ui/Badge";
import { ArrowLeftRight, CheckCircle } from "lucide-react";

export default function TransfersPage() {
  const apiKey = useApiKey();
  const [pending, setPending] = useState<PendingTransfer[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!apiKey) return;
    getPendingTransfers(apiKey)
      .then((data) => setPending(Array.isArray(data) ? data : []))
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey]);

  return (
    <>
      <PageHeader title="Transfers" subtitle="Attendees awaiting rebooking" />
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Student</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Email</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Original Course</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Cancelled</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={5} />)}
            {!loading && pending.map((t, i) => (
              <tr key={i} className="border-t border-gray-100 hover:bg-gray-50">
                <td className="px-4 py-3 font-medium text-gray-900">{t.studentName}</td>
                <td className="px-4 py-3"><a href={`mailto:${t.studentEmail}`} className="text-brand-600 hover:underline">{t.studentEmail}</a></td>
                <td className="px-4 py-3 text-gray-600 hidden md:table-cell">{t.courseTitle || t.courseCode}</td>
                <td className="px-4 py-3 text-gray-500 hidden md:table-cell">{formatDate(t.cancelledDate)}</td>
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
