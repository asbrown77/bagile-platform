"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useApiKey } from "@/lib/hooks/useApiKey";
import {
  CourseAttendee, CourseScheduleDetail, TransfersByCourse, PostCourseTemplate,
  getCourseAttendees, getCourseScheduleDetail, getTransfersByCourse,
  getPostCourseTemplate, removePrivateAttendee,
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
import { EditAttendeeModal } from "@/components/courses/EditAttendeeModal";
import { EditPrivateCoursePanel } from "@/components/courses/EditPrivateCoursePanel";
import { CourseContactsSection } from "@/components/courses/CourseContactsSection";
import { Download, Mail, Users, Calendar, User, UserPlus, Video, MapPin, FileText, ExternalLink, Send, Pencil, Trash2, Building2 } from "lucide-react";
import Link from "next/link";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk";

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
  const [showEditCourse, setShowEditCourse] = useState(false);
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
    } catch {
      setError("Failed to load course data");
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

  function emailAll() {
    const emails = active.map((a) => a.email).join(",");
    window.open(`mailto:${emails}?subject=${encodeURIComponent(course?.title || "")}`);
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

  // Build mailto with joining details
  function sendJoiningDetails() {
    const emails = active.map((a) => a.email).join(",");
    const courseName = course?.title || "";
    const dateStr = course?.startDate ? formatDate(course.startDate) : "";
    let body = `Hi,\n\nHere are the joining details for ${courseName} on ${dateStr}:\n\n`;
    if (isVirtual && course?.meetingUrl) {
      body += `Meeting Link: ${course.meetingUrl}\n`;
      if (course.meetingId) body += `Meeting ID: ${course.meetingId}\n`;
      if (course.meetingPasscode) body += `Passcode: ${course.meetingPasscode}\n`;
    }
    if (!isVirtual && course?.venueAddress) {
      body += `Venue: ${course.venueAddress}\n`;
    }
    body += `\nPlease let me know if you have any questions.\n\nBest regards`;
    window.open(`mailto:${emails}?subject=${encodeURIComponent(`Joining Details: ${courseName}`)}&body=${encodeURIComponent(body)}`);
  }

  // Derive client org name from course title (e.g. "PSM - Frazer-Nash (Bristol)" → "Frazer-Nash (Bristol)")
  function parseClientFromTitle(title: string | null | undefined): string | null {
    if (!title) return null;
    const dashIdx = title.indexOf(" - ");
    if (dashIdx === -1) return null;
    const after = title.slice(dashIdx + 3).trim();
    return after || null;
  }

  const clientName = isPrivate ? parseClientFromTitle(course?.title) : null;

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

      <PageHeader
        title={course?.title || `Course ${courseId}`}
        subtitle={course?.courseCode}
        actions={
          <div className="flex gap-2 flex-wrap">
            {isPrivate && (
              <>
                <Button variant="secondary" size="sm" onClick={() => setShowEditCourse(true)}>
                  <Pencil className="w-3.5 h-3.5" /> Edit Course
                </Button>
                <Button size="sm" onClick={() => setShowAddAttendees(true)}>
                  <UserPlus className="w-3.5 h-3.5" /> Add Attendees
                </Button>
              </>
            )}
            {active.length > 0 && (
              <>
                <Button variant="secondary" size="sm" onClick={downloadCsv}><Download className="w-3.5 h-3.5" /> Export CSV</Button>
                <Button variant="secondary" size="sm" onClick={emailAll}><Mail className="w-3.5 h-3.5" /> Email All</Button>
                {(course?.meetingUrl || course?.venueAddress) && (
                  <Button variant="secondary" size="sm" onClick={sendJoiningDetails}>
                    <Send className="w-3.5 h-3.5" /> Send Joining Details
                  </Button>
                )}
                <Button
                  size="sm"
                  onClick={() => setShowSendFollowUp(true)}
                  title={templateMissing ? `No template for ${course?.courseCode?.split("-")[0]?.toUpperCase()}` : "Send post-course follow-up email"}
                >
                  <Mail className="w-3.5 h-3.5" /> Send Follow-Up
                </Button>
              </>
            )}
          </div>
        }
      />

      {error && <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>}
      {successMsg && <AlertBanner variant="success" onDismiss={() => setSuccessMsg("")}>{successMsg}</AlertBanner>}

      {/* Course info header */}
      {loading ? (
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
          {[1, 2, 3, 4].map((i) => <SkeletonCard key={i} />)}
        </div>
      ) : course && (
        <>
          <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 mb-6">
            <div className="flex flex-wrap gap-x-6 gap-y-2 text-sm">
              <span className="flex items-center gap-1.5 text-gray-600">
                <Calendar className="w-3.5 h-3.5 text-gray-400" />
                {formatDate(course.startDate)}
                {course.endDate && course.endDate !== course.startDate && ` — ${formatDate(course.endDate)}`}
              </span>
              {course.trainerName && (
                <span className="flex items-center gap-1.5 text-gray-600">
                  <User className="w-3.5 h-3.5 text-gray-400" />
                  {course.trainerName}
                </span>
              )}
              <span className="flex items-center gap-1.5">
                <Badge variant={isVirtual ? "info" : "neutral"} dot>{isVirtual ? "Virtual" : "In-person"}</Badge>
              </span>
              <span className="flex items-center gap-1.5">
                <Badge variant={isPrivate ? "warning" : "success"} dot>{isPrivate ? "Private" : "Public"}</Badge>
              </span>
              {course.status && statusBadge(course.status)}
              {course.price && (
                <span className="text-gray-600">
                  {formatCurrency(course.price)} {isPrivate ? "total" : "per person"}
                </span>
              )}
              {course.capacity != null && (
                <span className={`font-medium ${isOverCapacity ? "text-red-600" : "text-gray-500"}`}>
                  Capacity: {isPrivate
                    ? <>{active.length}/{course.capacity}{isOverCapacity && " ⚠️"}</>
                    : course.capacity}
                </span>
              )}
              {course.invoiceReference && (
                <span className="flex items-center gap-1.5 text-gray-600">
                  <FileText className="w-3.5 h-3.5 text-gray-400" />
                  Invoice: {course.invoiceReference}
                </span>
              )}
              {/* Client org derived from course title */}
              {clientName && (
                <span className="flex items-center gap-1.5 text-gray-600">
                  <Building2 className="w-3.5 h-3.5 text-gray-400" />
                  Client: {clientName}
                </span>
              )}
            </div>

            {/* Meeting details (virtual) */}
            {isVirtual && course.meetingUrl && (
              <div className="mt-3 pt-3 border-t border-gray-100 flex flex-wrap gap-x-6 gap-y-2 text-sm">
                <span className="flex items-center gap-1.5 text-gray-600">
                  <Video className="w-3.5 h-3.5 text-blue-500" />
                  <a href={course.meetingUrl} target="_blank" rel="noopener" className="text-brand-600 hover:underline">
                    Join Meeting <ExternalLink className="w-3 h-3 inline" />
                  </a>
                </span>
                {course.meetingId && (
                  <span className="text-gray-500">ID: <span className="font-mono">{course.meetingId}</span></span>
                )}
                {course.meetingPasscode && (
                  <span className="text-gray-500">Passcode: <span className="font-mono">{course.meetingPasscode}</span></span>
                )}
              </div>
            )}

            {/* Venue details (in-person) */}
            {!isVirtual && course.venueAddress && (
              <div className="mt-3 pt-3 border-t border-gray-100 flex items-center gap-1.5 text-sm text-gray-600">
                <MapPin className="w-3.5 h-3.5 text-amber-500" />
                {course.venueAddress}
              </div>
            )}

            {/* Notes */}
            {course.notes && (
              <div className="mt-3 pt-3 border-t border-gray-100 text-sm text-gray-500">
                {course.notes}
              </div>
            )}
          </div>

          {/* KPI cards */}
          <div className={`grid ${isPrivate ? "grid-cols-1" : "grid-cols-3"} gap-4 mb-6`}>
            <Card label="Attendees" value={active.length} icon={<Users className="w-4 h-4" />} />
            {!isPrivate && <Card label="Orders" value={orders.length} />}
            {!isPrivate && <Card label="Revenue" value={formatCurrency(totalRevenue)} />}
          </div>

          {/* Course contacts — private courses only */}
          {isPrivate && (
            <CourseContactsSection apiKey={apiKey} courseId={courseId} />
          )}
        </>
      )}

      <div className="mt-6">
        <TabBar tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />
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
    </>
  );
}
