"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useApiKey } from "@/lib/hooks/useApiKey";
import { CourseAttendee, getCourseAttendees, formatCurrency } from "@/lib/api";
import { Card } from "@/components/ui/Card";
import { PageHeader } from "@/components/ui/PageHeader";
import { Button } from "@/components/ui/Button";
import { TabBar } from "@/components/ui/TabBar";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { EmptyState } from "@/components/ui/EmptyState";
import { SkeletonRow } from "@/components/ui/Skeleton";
import { statusBadge } from "@/components/ui/Badge";
import { Download, Mail, XCircle, Users } from "lucide-react";
import Link from "next/link";

const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk";

export default function CourseDetail() {
  const apiKey = useApiKey();
  const params = useParams();
  const courseId = Number(params.id);
  const [attendees, setAttendees] = useState<CourseAttendee[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [activeTab, setActiveTab] = useState("attendees");

  useEffect(() => {
    if (!apiKey || !courseId) return;
    loadData();
  }, [apiKey, courseId]);

  async function loadData() {
    try {
      setAttendees(await getCourseAttendees(apiKey, courseId));
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
        const code = courseCode.split("-")[0] || "course";
        const datePart = courseCode.split("-")[1] || courseId.toString();
        a.download = `${code}-Students-${datePart}.csv`;
        a.click();
        setSuccessMsg("CSV exported");
        setTimeout(() => setSuccessMsg(""), 3000);
      });
  }

  function emailAll() {
    const emails = active.map((a) => a.email).join(",");
    window.open(`mailto:${emails}?subject=${encodeURIComponent(courseName)}`);
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

  const courseName = attendees[0]?.courseName || `Course ${courseId}`;
  const courseCode = attendees[0]?.courseCode || "";
  const active = attendees.filter((a) => a.status === "active");
  const inactive = attendees.filter((a) => a.status !== "active");

  // Unique orders
  const orders = [...new Map(active.map((a) => [a.orderNumber, a])).values()].filter((a) => a.orderNumber);
  const totalRevenue = orders.reduce((s, o) => s + (o.orderAmount || 0), 0);

  const tabs = [
    { id: "attendees", label: "Attendees", count: active.length },
    { id: "orders", label: "Orders", count: orders.length },
    ...(inactive.length > 0 ? [{ id: "history", label: "Transferred / Refunded", count: inactive.length }] : []),
  ];

  return (
    <>
      <div className="mb-2">
        <Link href="/dashboard" className="text-sm text-brand-600 hover:text-brand-700">&larr; Dashboard</Link>
      </div>

      <PageHeader
        title={courseName}
        subtitle={courseCode}
        actions={
          <div className="flex gap-2">
            {active.length > 0 && (
              <>
                <Button variant="secondary" size="sm" onClick={downloadCsv}><Download className="w-3.5 h-3.5" /> Export CSV</Button>
                <Button variant="secondary" size="sm" onClick={emailAll}><Mail className="w-3.5 h-3.5" /> Email All</Button>
              </>
            )}
          </div>
        }
      />

      {error && <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>}
      {successMsg && <AlertBanner variant="success" onDismiss={() => setSuccessMsg("")}>{successMsg}</AlertBanner>}

      {/* KPI cards */}
      <div className="grid grid-cols-3 gap-4 mb-6">
        <Card label="Attendees" value={active.length} icon={<Users className="w-4 h-4" />} />
        <Card label="Orders" value={orders.length} />
        <Card label="Revenue" value={formatCurrency(totalRevenue)} />
      </div>

      <TabBar tabs={tabs} activeTab={activeTab} onChange={setActiveTab} />

      {/* Attendees tab */}
      {activeTab === "attendees" && (
        <div className="bg-white rounded-xl border border-gray-200 shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 border-b border-gray-200">
              <tr>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Name</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Email</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Organisation</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden lg:table-cell">Payment</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase hidden md:table-cell">Country</th>
                <th className="text-left px-4 py-3 text-xs font-medium text-gray-500 uppercase">Actions</th>
              </tr>
            </thead>
            <tbody>
              {loading && Array.from({ length: 4 }).map((_, i) => <SkeletonRow key={i} cols={6} />)}
              {!loading && active.map((a) => (
                <tr key={a.enrolmentId} className="border-t border-gray-100 hover:bg-gray-50">
                  <td className="px-4 py-2.5 font-medium text-gray-900">{a.firstName} {a.lastName}</td>
                  <td className="px-4 py-2.5">
                    <a href={`mailto:${a.email}?subject=${encodeURIComponent(courseName)}`} className="text-brand-600 hover:underline">{a.email}</a>
                  </td>
                  <td className="px-4 py-2.5 text-gray-600 hidden md:table-cell">{a.billingCompany || a.organisation || "—"}</td>
                  <td className="px-4 py-2.5 text-gray-500 text-xs hidden lg:table-cell">{a.paymentMethod || "—"}</td>
                  <td className="px-4 py-2.5 text-gray-500 hidden md:table-cell">{a.country || "—"}</td>
                  <td className="px-4 py-2.5 space-x-2">
                    <button onClick={() => markTransfer(a.enrolmentId)} className="text-amber-600 hover:text-amber-800 text-xs font-medium">Transfer</button>
                    <button onClick={() => markRefund(a.enrolmentId)} className="text-red-600 hover:text-red-800 text-xs font-medium">Refund</button>
                  </td>
                </tr>
              ))}
              {!loading && active.length === 0 && (
                <tr><td colSpan={6}>
                  <EmptyState icon={<Users className="w-10 h-10" />} title="No attendees" description="No active enrolments for this course" />
                </td></tr>
              )}
            </tbody>
          </table>
        </div>
      )}

      {/* Orders tab */}
      {activeTab === "orders" && (
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
