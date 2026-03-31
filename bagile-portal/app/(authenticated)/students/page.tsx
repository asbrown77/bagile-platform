"use client";

import { useEffect, useState, useCallback } from "react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { StudentListItem, getStudents, formatDate } from "@/lib/api";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { Users, Search } from "lucide-react";

export default function StudentsPage() {
  const apiKey = useApiKey();
  const [students, setStudents] = useState<StudentListItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const pageSize = 25;

  const loadStudents = useCallback(async () => {
    if (!apiKey) return;
    setLoading(true);
    try {
      const isEmail = search.includes("@");
      const result = await getStudents(apiKey, {
        ...(isEmail ? { email: search } : search ? { name: search } : {}),
        page,
        pageSize,
      });
      setStudents(result.items || []);
      setTotalCount(result.totalCount || 0);
    } catch {
      setStudents([]);
    } finally {
      setLoading(false);
    }
  }, [apiKey, search, page]);

  useEffect(() => { loadStudents(); }, [loadStudents]);

  // Debounce search
  const [searchInput, setSearchInput] = useState("");
  useEffect(() => {
    const timer = setTimeout(() => { setSearch(searchInput); setPage(1); }, 300);
    return () => clearTimeout(timer);
  }, [searchInput]);

  const totalPages = Math.ceil(totalCount / pageSize);

  return (
    <>
      <PageHeader title="Students" subtitle={`${totalCount} students`} />

      {/* Search */}
      <div className="relative mb-4">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
        <input
          type="text"
          placeholder="Search by name or email..."
          value={searchInput}
          onChange={(e) => setSearchInput(e.target.value)}
          className="w-full pl-10 pr-4 py-2 border border-gray-300 rounded-lg text-sm focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
        />
      </div>

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Name</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Email</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Company</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden lg:table-cell">Member Since</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 8 }).map((_, i) => <SkeletonRow key={i} cols={4} />)}
            {!loading && students.map((s) => (
              <tr key={s.id}
                className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors"
                onClick={() => window.location.href = `/students/${s.id}`}>
                <td className="px-4 py-3 font-medium text-gray-900">{s.fullName}</td>
                <td className="px-4 py-3 text-brand-600">{s.email}</td>
                <td className="px-4 py-3 text-gray-600 hidden md:table-cell">{s.company || "—"}</td>
                <td className="px-4 py-3 text-gray-500 hidden lg:table-cell">{formatDate(s.createdAt)}</td>
              </tr>
            ))}
            {!loading && students.length === 0 && (
              <tr><td colSpan={4}>
                <EmptyState
                  icon={<Users className="w-10 h-10" />}
                  title={search ? "No matches" : "No students"}
                  description={search ? `No students matching "${search}"` : "No student data found"}
                />
              </td></tr>
            )}
          </tbody>
        </table>

        {/* Pagination */}
        {!loading && totalPages > 1 && (
          <div className="flex items-center justify-between px-4 py-3 bg-gray-50 border-t border-gray-200">
            <span className="text-xs text-gray-500">
              Page {page} of {totalPages} ({totalCount} students)
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="px-3 py-1 text-xs rounded-lg border border-gray-300 bg-white hover:bg-gray-50 disabled:opacity-50"
              >
                Previous
              </button>
              <button
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="px-3 py-1 text-xs rounded-lg border border-gray-300 bg-white hover:bg-gray-50 disabled:opacity-50"
              >
                Next
              </button>
            </div>
          </div>
        )}
      </div>
    </>
  );
}
