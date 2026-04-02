"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  CourseAttendee, CourseScheduleDetail, TransfersByCourse, PostCourseTemplate,
  EmailSendLog,
  getCourseAttendees, getCourseScheduleDetail, getTransfersByCourse,
  getPostCourseTemplate, removePrivateAttendee, getEmailSendLog,
  formatCurrency, formatDate,
} from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { Button } from "@/components/ui/Button";
import { TabBar } from "@/components/ui/TabBar";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonCard, SkeletonRow } from "@/components/ui/Skeleton";
import { Badge, statusBadge } from "@/components/ui/Badge";
import { AddAttendeesPanel } from "@/components/courses/AddAttendeesPanel";
import { SendFollowUpPanel } from "@/components/courses/SendFollowUpPanel";
import { SendJoiningDetailsPanel } from "@/components/courses/SendJoiningDetailsPanel";
import { EmailSection } from "@/components/courses/EmailSection";
import { CourseBadge } from "@/components/courses/CourseBadge";
import { EditAttendeeModal } from "@/components/courses/EditAttendeeModal";
import { EditPrivateCoursePanel } from "@/components/courses/EditPrivateCoursePanel";
import { CourseContactsSection } from "@/components/courses/CourseContactsSection";
import { Download, Users, Calendar, User, UserPlus, Video, MapPin, FileText, ExternalLink, Pencil, Trash2, Building2 } from "lucide-react";
import Link from "next/link";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk";

// ── Capacity progress bar ────────────────────────────────────────────────────
function CapacityBar({ enrolled, capacity }: { enrolled: number; capacity: number }) {
  const pct = capacity > 0 ? Math.min((enrolled / capacity) * 100, 100) : 0;
  const colour = pct >= 75 ? "bg-green-500" : pct >= 50 ? "bg-amber-400" : "bg-red-500";
  const remaining = Math.max(capacity - enrolled, 0);
  return (
    <div>
      <div className="w-full bg-gray-200 rounded-full h-2 mb-1">
        <div className={`${colour} h-2 rounded-full transition-all`} style={{ width: `${pct}%` }} />
      </div>
      <span className="text-xs text-gray-500">
        {enrolled} / {capacity}{" "}
        {remaining > 0 ? `(${remaining} place${remaining === 1 ? "" : "s"} remaining)` : "(full)"}
      </span>
    </div>
  );
}

export default function CourseDetail() {
  const apiKey = useApiKey();
  const params = useParams();
  const courseId = Number(params.id);
  const [course, setCourse] = useState<CourseScheduleDetail | null>(null);
  const [attendees, setAttendees] = useState<CourseAttendee[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [activeTab, setActiveTab] = useState("attendees");
  const [showAddAttendees, setShowAddAttendees] = useState(false);
  const [showSendFollowUp, setShowSendFollowUp] = useState(false);
  const [showSendJoining, setShowSendJoining] = useState(false);
  const [showEditCourse, setShowEditCourse] = useState(false);
  const [emailLog, setEmailLog] = useState<EmailSendLog[]>([]);
  const [emailLogLoading, setEmailLogLoading] = useState(false);
  const [transfers, setTransfers] = useState<TransfersByCourse | null>(null);
  const [followUpTemplate, setFollowUpTemplate] = useState<PostCourseTemplate | null>(null);
  const [templateMissing, setTemplateMissing] = useState(false);
  const [editingAttendee, setEditingAttendee] = useState<CourseAttendee | null>(null);
  const [removingEnrolmentId, setRemovingEnrolmentId] = useState<number | null>(null);

  useEffect(() => {
    if (!apiKey || !courseId) return;
    loadData();
  }, [apiKey, courseId]);

  async function loadData() {
    setEmailLogLoading(true);
    try {
      const [detail, atts, xfers] = await Promise.all([
        getCourseScheduleDetail(apiKey, courseId),
        getCourseAttendees(apiKey, courseId),
        getTransfersByCourse(apiKey, courseId).catch(() => null),
      ]);
      setCourse(detail);
      setAttendees(atts);
      setTransfers(xfers);

      // Load follow-up template based on course code prefix
      const courseType = detail?.courseCode?.split("-")[0]?.toUpperCase();
      if (courseType) {
        getPostCourseTemplate(apiKey, courseType)
          .then((t) => { setFollowUpTemplate(t); setTemplateMissing(false); })
          .catch(() => { setFollowUpTemplate(null); setTemplateMissing(true); });
      }

      // Load email send log for the EmailSection status display
      getEmailSendLog(apiKey, courseId)
        .then(setEmailLog)
        .catch(() => setEmailLog([]))
        .finally(() => setEmailLogLoading(false));
    } catch {
      setError("Failed to load course data");
      setEmailLogLoading(false);
    } finally {
      setLoading(false);
    }
  }

  function downloadCsv() {
    fetch(`${API_URL}/api/course-schedules/${courseId}/attendees/export`, { headers: { "X-Api-Key": apiKey } })
      .then((r) => r.blob())
      .then((blob) => {
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        const code = (course?.courseCode || "").split("-")[0] || "course";
        const datePart = (course?.courseCode || "").split("-")[1] || courseId.toString();
        a.download = `${code}-Students-${datePart}.csv`;
        a.click();
        setSuccessMsg("CSV exported");
        setTimeout(() => setSuccessMsg(""), 3000);
      });
  }

  async function markRefund(enrolmentId: number) {
    if (!confirm("Mark as refunded?")) return;
    await fetch(`${API_URL}/api/enrolments/${enrolmentId}/mark-refund`, { method: "POST", headers: { "X-Api-Key": apiKey } });
    setSuccessMsg("Marked as refunded");
    loadData();
  }

  async function markTransfer(enrolmentId: number) {
    if (!confirm("Mark for transfer?")) return;
    await fetch(`${API_URL}/api/enrolments/${enrolmentId}/mark-transfer`, {
      method: "POST",
      headers: { "X-Api-Key": apiKey, "Content-Type": "application/json" },
      body: JSON.stringify({ reason: "provider_cancelled" }),
    });
    setSuccessMsg("Marked for transfer");
    loadData();
  }

  async function handleRemoveAttendee(attendee: CourseAttendee) {
    const name = `${attendee.firstName} ${attendee.lastName}`;
    if (!confirm(`Remove ${name} from this course?`)) return;
    setRemovingEnrolmentId(attendee.enrolmentId);
    try {
      await removePrivateAttendee(apiKey, courseId, attendee.enrolmentId);
      setSuccessMsg(`${name} removed`);
      loadData();
    } catch {
      setError("Failed to remove attendee — please try again");
    } finally {
      setRemovingEnrolmentId(null);
    }
  }

  // Derive client org name from course title (e.g. "PSM - Frazer-Nash (Bristol)" → "Frazer-Nash (Bristol)")
  function parseClientFromTitle(title: string | null | undefined): string | null {
    if (!title) return null;
    const dashIdx = title.indexOf(" - ");
    if (dashIdx === -1) return null;
    const after = title.slice(dashIdx + 3).trim();
    return after || null;
  }

  const isPrivate = course?.type === "private";
  const isVirtual = course?.formatType?.toLowerCase().includes("virtual");
  const active = attendees.filter((a) => a.status === "active");
  const inactive = attendees.filter((a) => a.status !== "active");
  const orders = [...new Map(active.map((a) => [a.orderNumber, a])).values()].filter((a) => a.orderNumber);
  const totalRevenue = orders.reduce((s, o) => s + (o.orderAmount || 0), 0);

  // Over-capacity: attendees vs stated capacity
  const isOverCapacity = isPrivate && course?.capacity != null && active.length > course.capacity;

  const transferCount = (transfers?.totalTransfersIn ?? 0) + (transfers?.totalTransfersOut ?? 0);
  const tabs = [
    { id: "attendees", label: "Attendees", count: active.length },
    ...(!isPrivate ? [{ id: "orders", label: "Orders", count: orders.length }] : []),
    ...(transferCount > 0 ? [{ id: "transfers", label: "Transfers", count: transferCount }] : []),
    ...(inactive.length > 0 ? [{ id: "history", label: "Transferred / Refunded", count: inactive.length }] : []),
  ];

  // Prefer the structured org relationship over title parsing (Sprint 23+)
  const clientName = isPrivate
    ? (course?.clientOrganisationName ?? parseClientFromTitle(course?.title))
    : null;
  const hasAttendees = active.length > 0;

  return (
    <>
      {/* Panels & modals */}
      <AddAttendeesPanel
        open={showAddAttendees}
        onClose={() => setShowAddAttendees(false)}
        apiKey={apiKey}
        courseId={courseId}
        onAdded={loadData}
        capacity={course?.capacity}
        currentAttendeeCount={active.length}
      />

      {course && (
        <SendFollowUpPanel
          open={showSendFollowUp}
          onClose={() => setShowSendFollowUp(false)}
          apiKey={apiKey}
          course={course}
          attendees={attendees}
          template={followUpTemplate}
          templateMissing={templateMissing}
        />
      )}

      {course && (
        <SendJoiningDetailsPanel
          open={showSendJoining}
          onClose={() => setShowSendJoining(false)}
          apiKey={apiKey}
          course={course}
          attendees={attendees}
        />
      )}

      {course && (
        <EditPrivateCoursePanel
          open={showEditCourse}
          onClose={() => setShowEditCourse(false)}
          apiKey={apiKey}
          course={course}
          onSaved={() => {
            setSuccessMsg("Course updated");
            loadData();
          }}
        />
      )}

      {editingAttendee && (
        <EditAttendeeModal
          attendee={editingAttendee}
          apiKey={apiKey}
          isPrivate={isPrivate}
          onSaved={() => {
            setSuccessMsg("Attendee updated");
            loadData();
          }}
          onClose={() => setEditingAttendee(null)}
        />
      )}

      <div className="mb-2">
        <Link href="/courses" className="text-sm text-brand-600 hover:text-brand-700">&larr; Courses</Link>
      </div>

      {/* ── Page header — badge + title + pencil edit icon (private only) ── */}
      <div className="flex items-start gap-2 mb-1">
        {course?.courseCode && (
          <div className="mt-0.5 flex-shrink-0">
            <CourseBadge courseCode={course.courseCode} size={48} />
          </div>
        )}
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <h1 className="text-xl font-semibold text-gray-900 truncate">
              {course?.title || `Course ${courseId}`}
            </h1>
            {isPrivate && course && (
              <button
                onClick={() => setShowEditCourse(true)}
                title="Edit course"
                className="text-gray-400 hover:text-brand-600 flex-shrink-0"
              >
                <Pencil className="w-4 h-4" />
              </button>
            )}
          </div>
          {course?.courseCode && (
            <p className="text-sm text-gray-500 mt-0.5">{course.courseCode}</p>
          )}
        </div>
        {/* Status badges */}
        <div className="flex items-center gap-2 flex-shrink-0 mt-1">
          <Badge variant={isPrivate ? "warning" : "success"} dot>{isPrivate ? "Private" : "Public"}</Badge>
          {course?.status && statusBadge(course.status)}
        </div>
      </div>

      {error && <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>}
      {successMsg && <AlertBanner variant="success" onDismiss={() => setSuccessMsg("")}>{successMsg}</AlertBanner>}

      {/* ── Two info cards: Details + Commercial/Enrolment ── */}
      {loading ? (
        <div className="grid grid-cols-2 gap-4 mb-6 mt-4">
          {[1, 2].map((i) => <SkeletonCard key={i} />)}
        </div>
      ) : course && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6 mt-4">

          {/* Left card — Details */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 space-y-2.5 text-sm">
            <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-1">Details</h2>

            <div className="flex items-start gap-2 text-gray-600">
              <Calendar className="w-3.5 h-3.5 text-gray-400 mt-0.5 flex-shrink-0" />
              <span>
                {formatDate(course.startDate)}
                {course.endDate && course.endDate !== course.startDate && ` — ${formatDate(course.endDate)}`}
              </span>
            </div>

            {course.trainerName && (
              <div className="flex items-center gap-2 text-gray-600">
                <User className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
                {course.trainerName}
              </div>
            )}

            <div className="flex items-center gap-2">
              <Badge variant={isVirtual ? "info" : "neutral"} dot>{isVirtual ? "Virtual" : "In-person"}</Badge>
            </div>

            {/* Venue / meeting details */}
            {isVirtual && course.meetingUrl && (
              <div className="flex items-start gap-2 text-gray-600">
                <Video className="w-3.5 h-3.5 text-blue-500 mt-0.5 flex-shrink-0" />
                <div>
                  <a href={course.meetingUrl} target="_blank" rel="noopener" className="text-brand-600 hover:underline">
                    Join Meeting <ExternalLink className="w-3 h-3 inline" />
                  </a>
                  {course.meetingId && (
                    <p className="text-xs text-gray-400 mt-0.5">ID: <span className="font-mono">{course.meetingId}</span>{course.meetingPasscode && ` · Passcode: ${course.meetingPasscode}`}</p>
                  )}
                </div>
              </div>
            )}
            {!isVirtual && course.venueAddress && (
              <div className="flex items-start gap-2 text-gray-600">
                <MapPin className="w-3.5 h-3.5 text-amber-500 mt-0.5 flex-shrink-0" />
                <span>{course.venueAddress}</span>
              </div>
            )}

            {/* Capacity */}
            {course.capacity != null && (
              <div className="pt-1">
                {isPrivate ? (
                  <span className={`font-medium ${isOverCapacity ? "text-blue-600" : "text-gray-500"}`}>
                    Capacity: {active.length}/{course.capacity}
                    {isOverCapacity && " (over — client approved)"}
                  </span>
                ) : (
                  <CapacityBar enrolled={active.length} capacity={course.capacity} />
                )}
              </div>
            )}

            {/* Notes */}
            {course.notes && (
              <div className="pt-2 border-t border-gray-100 text-gray-500">
                {course.notes}
              </div>
            )}
          </div>

          {/* Right card — Commercial (private) or Enrolment (public) */}
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 space-y-2.5 text-sm">
            <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-1">
              {isPrivate ? "Commercial" : "Enrolment"}
            </h2>

            {isPrivate ? (
              <>
                {course.price != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-gray-500">Total price</span>
                    <span className="font-semibold text-gray-900">{formatCurrency(course.price)}</span>
                  </div>
                )}
                {course.invoiceReference && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-1.5 text-gray-500">
                      <FileText className="w-3.5 h-3.5 text-gray-400" /> Invoice
                    </span>
                    <span className="font-mono text-gray-700">{course.invoiceReference}</span>
                  </div>
                )}
                {clientName && (
                  <div className="flex items-center justify-between">
                    <span className="flex items-center gap-1.5 text-gray-500">
                      <Building2 className="w-3.5 h-3.5 text-gray-400" /> Client
                    </span>
                    {course?.clientOrganisationName ? (
                      <a
                        href={`/organisations/${encodeURIComponent(course.clientOrganisationName)}`}
                        className="text-brand-600 hover:text-brand-700 hover:underline font-medium"
                      >
                        {clientName}
                      </a>
                    ) : (
                      <span className="text-gray-700">{clientName}</span>
                    )}
                  </div>
                )}
              </>
            ) : (
              <>
                {course.price != null && (
                  <div className="flex items-center justify-between">
                    <span className="text-gray-500">Per person</span>
                    <span className="font-semibold text-gray-900">{formatCurrency(course.price)}</span>
                  </div>
                )}
                {orders.length > 0 && (
                  <div className="flex items-center justify-between">
                    <span className="text-gray-500">Revenue</span>
                    <span className="font-semibold text-gray-900">{formatCurrency(totalRevenue)}</span>
                  </div>
                )}
                {course.capacity != null && (
                  <div className="pt-2">
                    <CapacityBar enrolled={active.length} capacity={course.capacity} />
                  </div>
                )}
                {course.capacity == null && (
                  <div className="flex items-center justify-between">
                    <span className="text-gray-500">Enrolled</span>
                    <span className="font-semibold text-gray-900">{active.length}</span>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}

      {/* ── KPI cards — public courses only ── */}
      {!loading && course && !isPrivate && (
        <div className="grid grid-cols-3 gap-4 mb-6">
          <Card label="Attendees" value={active.length} icon={<Users className="w-4 h-4" />} />
          <Card label="Orders" value={orders.length} />
          <Card label="Revenue" value={formatCurrency(totalRevenue)} />
        </div>
      )}

      {/* ── Emails section — two side-by-side cards ── */}
      {!loading && course && (
        <EmailSection
          emailLog={emailLog}
          logLoading={emailLogLoading}
          hasAttendees={hasAttendees}
          onOpenJoining={() => setShowSendJoining(true)}
          onOpenFollowUp={() => setShowSendFollowUp(true)}
        />
      )}

      {/* ── Contacts (private only) — between emails and attendee table ── */}
      {!loading && isPrivate && (
        <CourseContactsSection apiKey={apiKey} courseId={courseId} />
      )}

      {/* ── Attendee table header with inline Add + Export ── */}
      <div className="flex items-center justify-between mt-6 mb-2">
        <div>
          <TabBar tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />
        </div>
        {!loading && course && (
          <div className="flex items-center gap-3">
            {isPrivate && (
              <Button size="sm" onClick={() => setShowAddAttendees(true)}>
                <UserPlus className="w-3.5 h-3.5" /> Add Attendees
              </Button>
            )}
            {hasAttendees && (
              <button
                onClick={downloadCsv}
                className="text-xs text-gray-500 hover:text-brand-600 flex items-center gap-1"
              >
                <Download className="w-3 h-3" /> Export CSV
              </button>
            )}
          </div>
        )}
      </div>

      {/* Attendees tab */}
      {activeTab === "attendees" && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Name</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Email</th>
                {/* Organisation and Country columns — public courses only */}
                {!isPrivate && (
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Organisation</th>
                )}
                {!isPrivate && (
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Payment</th>
                )}
                {!isPrivate && (
                  <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Country</th>
                )}
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading && Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={isPrivate ? 3 : 6} />)}
              {!loading && active.map((a) => (
                <tr key={a.enrolmentId} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-2.5 font-medium text-gray-900">{a.firstName} {a.lastName}</td>
                  <td className="px-4 py-2.5">
                    <a href={`mailto:${a.email}?subject=${encodeURIComponent(course?.title || "")}`} className="text-brand-600 hover:underline">{a.email}</a>
                  </td>
                  {/* Public-only columns */}
                  {!isPrivate && (
                    <td className="px-4 py-2.5 text-gray-600 hidden md:table-cell">{a.billingCompany || a.organisation || "—"}</td>
                  )}
                  {!isPrivate && (
                    <td className="px-4 py-2.5 text-gray-500 text-xs hidden lg:table-cell">{a.paymentMethod || "—"}</td>
                  )}
                  {!isPrivate && (
                    <td className="px-4 py-2.5 text-gray-500 hidden md:table-cell">{a.country || "—"}</td>
                  )}
                  <td className="px-4 py-2.5 space-x-2">
                    {/* Edit attendee — always visible */}
                    <button
                      onClick={() => setEditingAttendee(a)}
                      className="text-gray-400 hover:text-brand-600 text-xs font-medium inline-flex items-center gap-1"
                      title="Edit attendee details"
                    >
                      <Pencil className="w-3 h-3" />
                    </button>
                    {/* Transfer + Refund — public courses only */}
                    {!isPrivate && (
                      <button onClick={() => markTransfer(a.enrolmentId)} className="text-amber-600 hover:text-amber-800 text-xs font-medium">Transfer</button>
                    )}
                    {!isPrivate && (
                      <button onClick={() => markRefund(a.enrolmentId)} className="text-red-600 hover:text-red-800 text-xs font-medium">Refund</button>
                    )}
                    {/* Remove — private courses only */}
                    {isPrivate && (
                      <button
                        onClick={() => handleRemoveAttendee(a)}
                        disabled={removingEnrolmentId === a.enrolmentId}
                        className="text-red-400 hover:text-red-600 disabled:opacity-40 inline-flex items-center gap-1"
                        title={`Remove ${a.firstName} ${a.lastName}`}
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    )}
                  </td>
                </tr>
              ))}
              {!loading && active.length === 0 && (
                <tr><td colSpan={isPrivate ? 3 : 6}>
                  <EmptyState icon={<Users className="w-10 h-10" />} title="No attendees" description="No active enrolments for this course" />
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Orders tab — only for public courses */}
      {activeTab === "orders" && !isPrivate && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Order #</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Billing Company</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Contact</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Payment</th>
                <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase">Attendees</th>
                <th className="text-right px-4 py-3 text-xs font-medium text-gray-500 uppercase">Amount</th>
                <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase">Status</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((o) => (
                <tr key={o.orderNumber} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-2.5 font-mono text-gray-700">{o.orderNumber}</td>
                  <td className="px-4 py-2.5 font-medium text-gray-900">{o.billingCompany || "—"}</td>
                  <td className="px-4 py-2.5 text-gray-600 hidden md:table-cell">
                    <p>{o.billingName || "—"}</p>
                    <p className="text-xs text-gray-400">{o.billingEmail}</p>
                  </td>
                  <td className="px-4 py-2.5 text-gray-500 text-xs hidden lg:table-cell">{o.paymentMethod || "—"}</td>
                  <td className="px-4 py-2.5 text-center text-gray-700">{o.orderAttendeeCount || "—"}</td>
                  <td className="px-4 py-2.5 text-right font-medium">{o.currency === "EUR" ? "€" : "£"}{(o.orderAmount || 0).toLocaleString()}</td>
                  <td className="px-4 py-2.5 text-center">{statusBadge(o.orderStatus || "pending")}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Transfers tab */}
      {activeTab === "transfers" && transfers && transferCount > 0 && (
        <div className="space-y-6">
          {transfers.transfersOut.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="px-5 py-3 border-b border-gray-200">
                <h3 className="text-sm font-semibold text-gray-900">Transferred Out ({transfers.transfersOut.length})</h3>
              </div>
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Student</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Transferred To</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Reason</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Date</th>
                  </tr>
                </thead>
                <tbody>
                  {transfers.transfersOut.map((t, i) => (
                    <tr key={i} className="border-t border-gray-100">
                      <td className="px-4 py-2.5">
                        <p className="font-medium text-gray-900">{t.studentName}</p>
                        <p className="text-xs text-gray-400">{t.studentEmail}</p>
                      </td>
                      <td className="px-4 py-2.5 text-gray-600">{t.toCourseTitle || t.toCourseCode || "Pending"}</td>
                      <td className="px-4 py-2.5 text-gray-500 text-xs hidden md:table-cell">{t.reason || "—"}</td>
                      <td className="px-4 py-2.5 text-gray-500 hidden md:table-cell">{formatDate(t.transferDate)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
          {transfers.transfersIn.length > 0 && (
            <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
              <div className="px-5 py-3 border-b border-gray-200">
                <h3 className="text-sm font-semibold text-gray-900">Transferred In ({transfers.transfersIn.length})</h3>
              </div>
              <table className="w-full text-sm">
                <thead className="bg-gray-50 border-b border-gray-200">
                  <tr>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Student</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Transferred From</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Reason</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Date</th>
                  </tr>
                </thead>
                <tbody>
                  {transfers.transfersIn.map((t, i) => (
                    <tr key={i} className="border-t border-gray-100">
                      <td className="px-4 py-2.5">
                        <p className="font-medium text-gray-900">{t.studentName}</p>
                        <p className="text-xs text-gray-400">{t.studentEmail}</p>
                      </td>
                      <td className="px-4 py-2.5 text-gray-600">{t.fromCourseTitle || t.fromCourseCode || "—"}</td>
                      <td className="px-4 py-2.5 text-gray-500 text-xs hidden md:table-cell">{t.reason || "—"}</td>
                      <td className="px-4 py-2.5 text-gray-500 hidden md:table-cell">{formatDate(t.transferDate)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* History tab */}
      {activeTab === "history" && inactive.length > 0 && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Name</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Email</th>
                <th className="text-center px-4 py-3 text-xs font-medium text-gray-500 uppercase">Status</th>
              </tr>
            </thead>
            <tbody>
              {inactive.map((a) => (
                <tr key={a.enrolmentId} className="border-t border-gray-100">
                  <td className="px-4 py-2.5 text-gray-500">{a.firstName} {a.lastName}</td>
                  <td className="px-4 py-2.5 text-gray-400">{a.email}</td>
                  <td className="px-4 py-2.5 text-center">{statusBadge(a.status)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* ── Email Send History — collapsible audit log ── */}
      {!loading && course && emailLog.length > 0 && (
        <div className="mt-6 bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <details>
            <summary className="w-full flex items-center justify-between px-5 py-3 text-sm font-medium text-gray-700 hover:bg-gray-50 cursor-pointer list-none">
              <span className="flex items-center gap-2">
                Send History
                <span className="text-xs text-gray-400 font-normal">({emailLog.length})</span>
              </span>
            </summary>
            <div className="border-t border-gray-200">
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Date</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase">Type</th>
                    <th className="text-left px-4 py-2 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Subject</th>
                    <th className="text-center px-4 py-2 text-xs font-medium text-gray-500 uppercase">Recipients</th>
                  </tr>
                </thead>
                <tbody>
                  {emailLog.map((log) => (
                    <tr key={log.id} className="border-t border-gray-100">
                      <td className="px-4 py-2.5 text-gray-600 whitespace-nowrap">
                        {new Date(log.sentAt).toLocaleDateString("en-GB", { day: "numeric", month: "short", year: "numeric" })}
                        <span className="text-gray-400 text-xs ml-1">
                          {new Date(log.sentAt).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit" })}
                        </span>
                      </td>
                      <td className="px-4 py-2.5">
                        <span className="inline-flex items-center gap-1.5">
                          <span className={`text-xs font-medium ${log.templateType === "pre_course" ? "text-blue-700" : "text-purple-700"}`}>
                            {log.templateType === "pre_course" ? "Joining Details" : "Follow-Up"}
                          </span>
                          {log.isTest && (
                            <span className="text-xs bg-amber-100 text-amber-700 px-1.5 py-0.5 rounded font-medium">[TEST]</span>
                          )}
                        </span>
                      </td>
                      <td className="px-4 py-2.5 text-gray-500 text-xs hidden md:table-cell truncate max-w-xs">{log.subject}</td>
                      <td className="px-4 py-2.5 text-center text-gray-700">{log.isTest ? "—" : log.recipientCount}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </details>
        </div>
      )}
    </>
  );
}
