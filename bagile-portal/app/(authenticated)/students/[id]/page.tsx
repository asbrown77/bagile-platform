"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  StudentDetail, StudentEnrolment,
  getStudentDetail, getStudentEnrolments, formatDate,
} from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonCard, SkeletonRow } from "@/components/ui/Skeleton";
import { Badge, statusBadge } from "@/components/ui/Badge";
import { Users, GraduationCap, Building2, Calendar, Mail } from "lucide-react";
import Link from "next/link";

export default function StudentDetailPage() {
  const apiKey = useApiKey();
  const params = useParams();
  const studentId = Number(params.id);
  const [student, setStudent] = useState<StudentDetail | null>(null);
  const [enrolments, setEnrolments] = useState<StudentEnrolment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!apiKey || !studentId) return;
    Promise.all([
      getStudentDetail(apiKey, studentId),
      getStudentEnrolments(apiKey, studentId),
    ])
      .then(([detail, enrols]) => {
        setStudent(detail);
        setEnrolments(enrols);
      })
      .catch(() => {})
      .finally(() => setLoading(false));
  }, [apiKey, studentId]);

  return (
    <>
      <div className="mb-2">
        <Link href="/students" className="text-sm text-brand-600 hover:text-brand-700">&larr; Students</Link>
      </div>

      <PageHeader
        title={student?.fullName || `Student ${studentId}`}
        subtitle={student?.email}
        actions={student ? (
          <a href={`mailto:${student.email}`}
            className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-brand-600 rounded-lg hover:bg-brand-700 shadow-sm">
            <Mail className="w-3.5 h-3.5" /> Email
          </a>
        ) : undefined}
      />

      {/* KPI Cards */}
      {loading ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          {[1, 2, 3, 4].map((i) => <SkeletonCard key={i} />)}
        </div>
      ) : student && (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          <Card label="Enrolments" value={student.totalEnrolments} icon={<GraduationCap className="w-4 h-4" />} />
          <Card label="Company" value={student.company || "—"} icon={<Building2 className="w-4 h-4" />} />
          <Card label="Last Course" value={student.lastCourseDate ? formatDate(student.lastCourseDate) : "—"} icon={<Calendar className="w-4 h-4" />} />
          <Card label="Member Since" value={formatDate(student.createdAt)} icon={<Users className="w-4 h-4" />} />
        </div>
      )}

      {/* Enrolment Timeline */}
      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="px-5 py-3 border-b border-gray-200">
          <h2 className="text-sm font-semibold text-gray-900">Course History</h2>
        </div>
        <table className="w-full text-sm">
          <thead className="bg-gray-50 border-b border-gray-200">
            <tr>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Course</th>
              <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Date</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide">Status</th>
              <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase tracking-wide hidden md:table-cell">Type</th>
            </tr>
          </thead>
          <tbody>
            {loading && Array.from({ length: 3 }).map((_, i) => <SkeletonRow key={i} cols={4} />)}
            {!loading && enrolments.map((e) => (
              <tr key={e.enrolmentId}
                className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer transition-colors"
                onClick={() => window.location.href = `/courses/${e.courseScheduleId}`}>
                <td className="px-4 py-3">
                  <p className="font-medium text-gray-900">{e.courseTitle}</p>
                  <p className="text-xs text-gray-400 font-mono">{e.courseCode}</p>
                </td>
                <td className="px-4 py-3 text-gray-600">{formatDate(e.courseStartDate)}</td>
                <td className="px-4 py-3 text-center">{statusBadge(e.status.toLowerCase())}</td>
                <td className="px-4 py-3 text-center hidden md:table-cell">
                  {e.type && <Badge variant={e.type === "public" ? "success" : "warning"} dot>{e.type === "public" ? "Public" : "Private"}</Badge>}
                </td>
              </tr>
            ))}
            {!loading && enrolments.length === 0 && (
              <tr><td colSpan={4}>
                <EmptyState icon={<GraduationCap className="w-10 h-10" />} title="No courses" description="This student has no enrolments yet" />
              </td></tr>
            )}
          </tbody>
        </table>
      </div>
    </>
  );
}
