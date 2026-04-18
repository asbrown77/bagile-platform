"use client";

import { useEffect, useState } from "react";
import { ClipboardList, CheckCircle2 } from "lucide-react";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { PlannedCourse, CoursePublication, listPlannedCourses, formatDate } from "@/lib/api";
import { getBadgeSrc, getCourseCodeDisplay, getCourseDisplayName, getStatusLabel, getStatusBadgeVariant } from "@/lib/calendarHelpers";
import { PageHeader } from "@/components/ui/PageHeader";
import { Badge } from "@/components/ui/Badge";
import { SkeletonRow } from "@/components/ui/Skeleton";

function GatewayBadge({ label, pub }: { label: string; pub: CoursePublication | undefined }) {
  if (pub) {
    return pub.externalUrl ? (
      <a
        href={pub.externalUrl}
        target="_blank"
        rel="noopener noreferrer"
        className="inline-flex items-center gap-1 text-xs text-green-700 font-medium hover:underline"
        title={`Published ${pub.publishedAt ? new Date(pub.publishedAt).toLocaleDateString("en-GB") : ""}`}
      >
        <CheckCircle2 className="w-3.5 h-3.5 shrink-0" />
        {label}
      </a>
    ) : (
      <span className="inline-flex items-center gap-1 text-xs text-green-700 font-medium">
        <CheckCircle2 className="w-3.5 h-3.5 shrink-0" />
        {label}
      </span>
    );
  }
  return (
    <span className="inline-flex items-center gap-1 text-xs text-gray-400">
      <span className="w-3.5 h-3.5 flex items-center justify-center shrink-0">–</span>
      {label}
    </span>
  );
}

function PlannedCourseRow({ course }: { course: PlannedCourse }) {
  const courseType = course.courseType;
  const badgeSrc = getBadgeSrc(courseType);
  const codeDisplay = getCourseCodeDisplay(courseType);
  const courseName = getCourseDisplayName(courseType);
  const status = course.status ?? "planned";

  const ecommercePub = course.publications?.find((p) => p.gateway === "ecommerce");
  const scrumorgPub = course.publications?.find((p) => p.gateway === "scrumorg");

  return (
    <tr className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
      <td className="px-4 py-3">
        <div className="flex items-center gap-3">
          {badgeSrc ? (
            <img src={badgeSrc} alt={codeDisplay} className="h-8 w-8 object-contain shrink-0" />
          ) : (
            <div className="h-8 w-8 rounded bg-gray-200 shrink-0" />
          )}
          <div>
            <div className="text-sm font-medium text-gray-800">{codeDisplay}</div>
            <div className="text-xs text-gray-500">
              {courseName !== courseType ? courseName : ""}
            </div>
          </div>
        </div>
      </td>
      <td className="px-4 py-3 text-sm text-gray-700">
        {courseName}
        {course.isPrivate && (
          <span className="ml-2 inline-flex items-center px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider rounded bg-amber-100 text-amber-700">
            Private
          </span>
        )}
      </td>
      <td className="px-4 py-3 text-sm text-gray-600">
        {formatDate(course.startDate)}
        {course.endDate && course.endDate !== course.startDate
          ? ` – ${formatDate(course.endDate)}`
          : ""}
      </td>
      <td className="px-4 py-3 text-sm text-gray-600">
        {course.trainerName ?? "—"}
      </td>
      <td className="px-4 py-3">
        <div className="flex flex-col gap-1">
          <GatewayBadge label="WooCommerce" pub={ecommercePub} />
          <GatewayBadge label="Scrum.org" pub={scrumorgPub} />
        </div>
      </td>
      <td className="px-4 py-3">
        <Badge variant={getStatusBadgeVariant(status)} dot>
          {getStatusLabel(status)}
        </Badge>
      </td>
    </tr>
  );
}

export default function PlannedCoursesPage() {
  const apiKey = useApiKey() ?? "";
  const [courses, setCourses] = useState<PlannedCourse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    if (!apiKey) return;
    setLoading(true);
    setError("");
    listPlannedCourses(apiKey)
      .then(setCourses)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [apiKey]);

  return (
    <div className="p-6 max-w-6xl mx-auto">
      <PageHeader
        title="Planned Courses"
        subtitle="Courses scheduled in the portal and their publication status"
      />

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 border border-red-200 px-4 py-3 text-sm text-red-700">
          {error}
        </div>
      )}

      <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left">
            <thead>
              <tr className="border-b border-gray-200 text-xs font-semibold uppercase tracking-wider text-gray-500">
                <th className="px-4 py-3">Course Code</th>
                <th className="px-4 py-3">Title</th>
                <th className="px-4 py-3">Start Date</th>
                <th className="px-4 py-3">Trainer</th>
                <th className="px-4 py-3">Gateways</th>
                <th className="px-4 py-3">Status</th>
              </tr>
            </thead>
            <tbody>
              {loading && (
                <>
                  <SkeletonRow cols={6} />
                  <SkeletonRow cols={6} />
                  <SkeletonRow cols={6} />
                </>
              )}
              {!loading && courses.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-12 text-center">
                    <div className="flex flex-col items-center gap-2 text-gray-400">
                      <ClipboardList className="w-8 h-8" />
                      <p className="text-sm">No planned courses yet.</p>
                    </div>
                  </td>
                </tr>
              )}
              {!loading && courses.map((c) => <PlannedCourseRow key={c.id} course={c} />)}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
