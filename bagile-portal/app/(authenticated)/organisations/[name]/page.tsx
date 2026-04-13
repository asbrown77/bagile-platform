"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  OrganisationDetail, OrgCourseHistory, CourseAttendee, OrgConfig,
  getOrganisationDetail, getOrganisationCourseHistory, getCourseAttendees,
  getOrgConfig, updateOrgConfig,
  formatCurrency, formatDate
} from "@/lib/api";
import { SlideOver } from "@/components/ui/SlideOver";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonCard, SkeletonRow } from "@/components/ui/Skeleton";
import { Badge } from "@/components/ui/Badge";
import { Building2, ShoppingCart, Users, TrendingUp, Calendar, Settings } from "lucide-react";
import Link from "next/link";

export default function OrganisationDetailPage() {
  const apiKey = useApiKey();
  const params = useParams();
  const orgName = decodeURIComponent(String(params.name));
  const [org, setOrg] = useState<OrganisationDetail | null>(null);
  const [history, setHistory] = useState<OrgCourseHistory[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [yearFilter, setYearFilter] = useState<string>("all");
  const [allTimeOrg, setAllTimeOrg] = useState<OrganisationDetail | null>(null);
  const [selectedCourse, setSelectedCourse] = useState<OrgCourseHistory | null>(null);
  const [courseAttendees, setCourseAttendees] = useState<CourseAttendee[]>([]);
  const [attendeesLoading, setAttendeesLoading] = useState(false);

  // Config slide-over state
  const [orgConfig, setOrgConfig] = useState<OrgConfig | null>(null);
  const [configOpen, setConfigOpen] = useState(false);
  const [configAliases, setConfigAliases] = useState<string[]>([]);
  const [configDomain, setConfigDomain] = useState("");
  const [newAlias, setNewAlias] = useState("");
  const [configSaving, setConfigSaving] = useState(false);

  // All-time fetch — only for Relationship KPI, unaffected by year filter
  useEffect(() => {
    if (!apiKey || !orgName) return;
    getOrganisationDetail(apiKey, orgName, undefined).then(setAllTimeOrg).catch(() => null);
  }, [apiKey, orgName]);

  // Config fetch — keyed on orgName only; config doesn't change with year
  useEffect(() => {
    if (!apiKey || !orgName) return;
    getOrgConfig(apiKey, orgName).then(setOrgConfig);
  }, [apiKey, orgName]);

  useEffect(() => {
    if (!apiKey || !orgName) return;
    setLoading(true);
    const yearNum = yearFilter === "all" ? undefined : Number(yearFilter);
    Promise.all([
      getOrganisationDetail(apiKey, orgName, yearNum).catch(() => null),
      getOrganisationCourseHistory(apiKey, orgName, yearNum).catch(() => []),
    ])
      .then(([detail, courses]) => {
        setOrg(detail);
        setHistory(courses);
      })
      .catch(() => setError("Failed to load organisation"))
      .finally(() => setLoading(false));
  }, [apiKey, orgName, yearFilter]);

  async function handleCourseClick(course: OrgCourseHistory) {
    setSelectedCourse(course);
    setAttendeesLoading(true);
    setCourseAttendees([]);
    try {
      const attendees = await getCourseAttendees(apiKey!, course.courseScheduleId, orgName);
      setCourseAttendees(attendees);
    } finally {
      setAttendeesLoading(false);
    }
  }

  function openConfig() {
    setConfigAliases(orgConfig?.aliases ?? []);
    setConfigDomain(orgConfig?.primaryDomain ?? "");
    setConfigOpen(true);
  }

  async function saveConfig() {
    if (!orgConfig) return;
    setConfigSaving(true);
    try {
      const updated = await updateOrgConfig(
        apiKey!, orgName,
        configAliases.map(a => a.trim()).filter(Boolean),
        configDomain.trim() || null,
      );
      if (updated) {
        setOrgConfig(updated);
        // Re-trigger year-filtered fetch in case aliases changed how org resolves
        setYearFilter(f => f);
      }
      setConfigOpen(false);
    } finally {
      setConfigSaving(false);
    }
  }

  const relationshipDays = allTimeOrg?.firstOrderDate
    ? Math.round((Date.now() - new Date(allTimeOrg.firstOrderDate).getTime()) / (1000 * 60 * 60 * 24))
    : 0;

  return (
    <>
      <div className="mb-2">
        <Link href="/organisations" className="text-sm text-brand-600 hover:text-brand-700">&larr; Organisations</Link>
      </div>

      <PageHeader
        title={orgName}
        subtitle={org?.primaryDomain ? `${org.primaryDomain}${yearFilter !== "all" ? ` — ${yearFilter}` : ""}` : undefined}
        actions={
          <div className="flex items-center gap-2">
            <button
              onClick={openConfig}
              title="Configure organisation"
              className="p-1.5 rounded-lg border border-gray-300 bg-white text-gray-500 hover:text-gray-700 hover:bg-gray-50 transition-colors"
            >
              <Settings className="w-4 h-4" />
            </button>
            <div className="inline-flex rounded-lg border border-gray-300 overflow-hidden">
              {["all", "2026", "2025", "2024"].map((opt) => (
                <button key={opt} onClick={() => setYearFilter(opt)}
                  className={`px-3 py-1.5 text-xs font-medium transition-colors ${
                    yearFilter === opt ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"
                  } ${opt !== "all" ? "border-l border-gray-300" : ""}`}>
                  {opt === "all" ? "All time" : opt}
                </button>
              ))}
            </div>
          </div>
        }
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
          <Card label="Enrolments" value={org.totalEnrolments} icon={<Users className="w-4 h-4" />} />
          <Card
            label="Relationship"
            value={relationshipDays > 365 ? `${Math.round(relationshipDays / 365)}y` : `${relationshipDays}d`}
            subtitle={allTimeOrg?.firstOrderDate ? `Since ${formatDate(allTimeOrg.firstOrderDate)}` : "—"}
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
              <tr key={h.courseCode} onClick={() => handleCourseClick(h)} className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer">
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

      {/* Attendees slide-over */}
      <SlideOver
        open={!!selectedCourse}
        onClose={() => setSelectedCourse(null)}
        title={selectedCourse?.courseTitle || selectedCourse?.courseCode || ""}
        subtitle={`${orgName} attendees`}
      >
        {attendeesLoading ? (
          <div className="space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="animate-pulse">
                <div className="h-4 bg-gray-200 rounded w-3/4 mb-1" />
                <div className="h-3 bg-gray-100 rounded w-1/2" />
              </div>
            ))}
          </div>
        ) : courseAttendees.length === 0 ? (
          <EmptyState
            icon={<Users className="w-10 h-10" />}
            title="No attendees"
            description="No attendees from this organisation on this course"
          />
        ) : (
          <div className="divide-y divide-gray-100">
            {courseAttendees.map((a) => (
              <div key={a.enrolmentId} className="py-3 first:pt-0">
                <div className="flex items-center justify-between">
                  <p className="text-sm font-medium text-gray-900">{a.name}</p>
                  <StatusBadge status={a.status} />
                </div>
                <p className="text-xs text-gray-500 mt-0.5">{a.email}</p>
              </div>
            ))}
          </div>
        )}
      </SlideOver>

      {/* Configure organisation slide-over */}
      <SlideOver
        open={configOpen}
        onClose={() => setConfigOpen(false)}
        title="Configure Organisation"
        subtitle={orgName}
      >
        <div className="space-y-5">
          {/* Primary Domain */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Primary Domain</label>
            <input
              type="text"
              value={configDomain}
              onChange={e => setConfigDomain(e.target.value)}
              placeholder="e.g. example.com"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
            />
            <p className="text-xs text-gray-400 mt-1">Shown on the org detail page. Leave blank to auto-detect from student emails.</p>
          </div>

          {/* Aliases */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-2">Billing Company Aliases</label>
            <p className="text-xs text-gray-400 mb-3">All billing_company values that should map to this organisation. The analytics queries use these to consolidate orders.</p>

            {/* Existing aliases as chips */}
            <div className="flex flex-wrap gap-2 mb-3">
              {configAliases.map((alias, i) => (
                <span key={i} className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs bg-gray-100 text-gray-700 border border-gray-200">
                  {alias}
                  <button
                    onClick={() => setConfigAliases(prev => prev.filter((_, j) => j !== i))}
                    className="text-gray-400 hover:text-red-500 ml-0.5"
                  >
                    &times;
                  </button>
                </span>
              ))}
              {configAliases.length === 0 && (
                <p className="text-xs text-gray-400 italic">No aliases yet</p>
              )}
            </div>

            {/* Add alias input */}
            <div className="flex gap-2">
              <input
                type="text"
                value={newAlias}
                onChange={e => setNewAlias(e.target.value)}
                onKeyDown={e => {
                  if (e.key === "Enter" && newAlias.trim()) {
                    setConfigAliases(prev => [...prev, newAlias.trim()]);
                    setNewAlias("");
                  }
                }}
                placeholder="Type alias and press Enter"
                className="flex-1 border border-gray-300 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-500"
              />
              <button
                onClick={() => {
                  if (newAlias.trim()) {
                    setConfigAliases(prev => [...prev, newAlias.trim()]);
                    setNewAlias("");
                  }
                }}
                className="px-3 py-2 bg-brand-600 text-white rounded-lg text-sm hover:bg-brand-700"
              >
                Add
              </button>
            </div>
          </div>

          {/* Note if org not in table */}
          {!orgConfig && (
            <p className="text-xs text-amber-600 bg-amber-50 border border-amber-200 rounded-lg px-3 py-2">
              This organisation is not in the organisations table yet. Configuration will only work for registered organisations.
            </p>
          )}

          {/* Save */}
          <div className="pt-2 border-t border-gray-100">
            <button
              onClick={saveConfig}
              disabled={configSaving || !orgConfig}
              className="w-full py-2 bg-brand-600 text-white rounded-lg text-sm font-medium hover:bg-brand-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {configSaving ? "Saving\u2026" : "Save changes"}
            </button>
          </div>
        </div>
      </SlideOver>
    </>
  );
}

function StatusBadge({ status }: { status: string }) {
  const s = status.toLowerCase();
  let color = "bg-gray-100 text-gray-700";
  if (s === "active" || s === "completed") color = "bg-green-50 text-green-700";
  else if (s === "cancelled") color = "bg-red-50 text-red-700";
  else if (s === "transferred" || s === "pending_transfer") color = "bg-amber-50 text-amber-700";

  return (
    <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${color}`}>
      {status}
    </span>
  );
}
