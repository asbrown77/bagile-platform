"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { CourseAttendee, getCourseAttendees } from "@/lib/api";

const API_KEY = process.env.NEXT_PUBLIC_BAGILE_API_KEY || "";
const API_URL = process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk";

export default function CourseDetail() {
  const params = useParams();
  const courseId = Number(params.id);
  const [attendees, setAttendees] = useState<CourseAttendee[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [showCancelDialog, setShowCancelDialog] = useState(false);
  const [cancelActions, setCancelActions] = useState<Record<number, string>>({});
  const [actionMessage, setActionMessage] = useState("");

  useEffect(() => {
    if (!API_KEY) { setError("API key not configured"); setLoading(false); return; }
    loadData();
  }, [courseId]);

  async function loadData() {
    try {
      setAttendees(await getCourseAttendees(API_KEY, courseId));
    } catch {
      setError("Failed to load data");
    } finally {
      setLoading(false);
    }
  }

  function downloadCsv() {
    fetch(`${API_URL}/api/course-schedules/${courseId}/attendees/export`, { headers: { "X-Api-Key": API_KEY } })
      .then((r) => r.blob())
      .then((blob) => {
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        const code = courseCode.split("-")[0] || "course";
        const datePart = courseCode.split("-")[1] || courseId.toString();
        a.download = `${code}-Students-${datePart}.csv`;
        a.click();
      });
  }

  function emailAll() {
    const emails = active.map((a) => a.email).join(",");
    window.open(`mailto:${emails}?subject=${encodeURIComponent(courseName)}`);
  }

  async function markRefund(enrolmentId: number) {
    if (!confirm("Mark as refunded?")) return;
    await fetch(`${API_URL}/api/enrolments/${enrolmentId}/mark-refund`, { method: "POST", headers: { "X-Api-Key": API_KEY } });
    setActionMessage("Marked as refunded");
    loadData();
  }

  async function markTransfer(enrolmentId: number) {
    if (!confirm("Mark for transfer?")) return;
    await fetch(`${API_URL}/api/enrolments/${enrolmentId}/mark-transfer`, {
      method: "POST",
      headers: { "X-Api-Key": API_KEY, "Content-Type": "application/json" },
      body: JSON.stringify({ reason: "provider_cancelled" }),
    });
    setActionMessage("Marked for transfer");
    loadData();
  }

  async function cancelCourse() {
    const actions = Object.entries(cancelActions).map(([id, action]) => ({
      enrolmentId: Number(id),
      action,
    }));
    await fetch(`${API_URL}/api/course-schedules/${courseId}/cancel-with-actions`, {
      method: "POST",
      headers: { "X-Api-Key": API_KEY, "Content-Type": "application/json" },
      body: JSON.stringify({ attendeeActions: actions }),
    });
    setShowCancelDialog(false);
    setActionMessage("Course cancelled");
    loadData();
  }

  const courseName = attendees[0]?.courseName || `Course ${courseId}`;
  const courseCode = attendees[0]?.courseCode || "";
  const active = attendees.filter((a) => a.status === "active");
  const transferred = attendees.filter((a) => a.status === "transferred" || a.status === "pending_transfer");
  const refunded = attendees.filter((a) => a.status === "refunded");

  // Unique orders
  const orders = [...new Map(active.map((a) => [a.orderNumber, a])).values()].filter((a) => a.orderNumber);

  return (
    <div className="max-w-5xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <a href="/dashboard" className="text-sm text-blue-600 hover:underline mb-1 inline-block">&larr; Dashboard</a>
          <h1 className="text-2xl font-bold text-gray-900">{courseName}</h1>
          {courseCode && <p className="text-gray-500 text-sm">{courseCode}</p>}
        </div>
        <div className="flex gap-2">
          {active.length > 0 && (
            <>
              <button onClick={downloadCsv} className="bg-blue-600 text-white px-3 py-2 rounded text-sm hover:bg-blue-700">Export CSV</button>
              <button onClick={emailAll} className="bg-gray-600 text-white px-3 py-2 rounded text-sm hover:bg-gray-700">Email All</button>
            </>
          )}
          <button onClick={() => setShowCancelDialog(true)} className="bg-red-600 text-white px-3 py-2 rounded text-sm hover:bg-red-700">Cancel Course</button>
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>}
      {actionMessage && <div className="bg-green-50 border border-green-200 text-green-700 px-4 py-3 rounded mb-6">{actionMessage} <button onClick={() => setActionMessage("")} className="ml-2 underline text-sm">dismiss</button></div>}

      {loading ? <p className="text-gray-500">Loading...</p> : (
        <>
          {/* Summary */}
          <div className="grid grid-cols-3 gap-4 mb-6">
            <div className="bg-white border rounded-lg p-4"><p className="text-xs text-gray-500">Attendees</p><p className="text-xl font-bold">{active.length}</p></div>
            <div className="bg-white border rounded-lg p-4"><p className="text-xs text-gray-500">Orders</p><p className="text-xl font-bold">{orders.length}</p></div>
            <div className="bg-white border rounded-lg p-4"><p className="text-xs text-gray-500">Revenue</p><p className="text-xl font-bold">£{orders.reduce((s, o) => s + (o.orderAmount || 0), 0).toLocaleString()}</p></div>
          </div>

          {/* Attendees */}
          <h2 className="font-semibold text-gray-900 mb-2">Attendees</h2>
          <div className="bg-white rounded-lg shadow-sm border overflow-hidden mb-6">
            {active.length === 0 ? (
              <p className="p-6 text-gray-500 text-center">No attendees</p>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Name</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Email</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Organisation</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Country</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Order</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {active.map((a) => (
                    <tr key={a.enrolmentId} className="border-t hover:bg-gray-50">
                      <td className="px-4 py-3 font-medium text-gray-900">{a.firstName} {a.lastName}</td>
                      <td className="px-4 py-3"><a href={`mailto:${a.email}?subject=${encodeURIComponent(courseName)}`} className="text-blue-600 hover:underline">{a.email}</a></td>
                      <td className="px-4 py-3 text-gray-600">{a.organisation || "—"}</td>
                      <td className="px-4 py-3 text-gray-600">{a.country || "—"}</td>
                      <td className="px-4 py-3 text-gray-500 font-mono text-xs">{a.orderNumber || "—"}</td>
                      <td className="px-4 py-3 space-x-2">
                        <button onClick={() => window.open(`mailto:${a.email}?subject=${encodeURIComponent(courseName)}`)} className="text-blue-600 hover:text-blue-800 text-xs">Email</button>
                        <button onClick={() => markTransfer(a.enrolmentId)} className="text-amber-600 hover:text-amber-800 text-xs">Transfer</button>
                        <button onClick={() => markRefund(a.enrolmentId)} className="text-red-600 hover:text-red-800 text-xs">Refund</button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {/* Orders */}
          {orders.length > 0 && (
            <>
              <h2 className="font-semibold text-gray-900 mb-2">Orders</h2>
              <div className="bg-white rounded-lg shadow-sm border overflow-hidden mb-6">
                <table className="w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Order #</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Amount</th>
                      <th className="text-left px-4 py-2 font-medium text-gray-600">Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {orders.map((o) => (
                      <tr key={o.orderNumber} className="border-t hover:bg-gray-50">
                        <td className="px-4 py-3 font-mono">{o.orderNumber}</td>
                        <td className="px-4 py-3">{o.currency === "EUR" ? "€" : "£"}{(o.orderAmount || 0).toLocaleString()}</td>
                        <td className="px-4 py-3">
                          <span className={`px-2 py-1 rounded-full text-xs font-medium ${o.orderStatus === "completed" ? "bg-green-100 text-green-700" : "bg-amber-100 text-amber-700"}`}>{o.orderStatus}</span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {/* Transferred / Refunded */}
          {(transferred.length > 0 || refunded.length > 0) && (
            <>
              <h2 className="font-semibold text-gray-500 mb-2">Transferred / Refunded</h2>
              <div className="bg-gray-50 rounded-lg border overflow-hidden mb-6">
                <table className="w-full text-sm">
                  <tbody>
                    {[...transferred, ...refunded].map((a) => (
                      <tr key={a.enrolmentId} className="border-t">
                        <td className="px-4 py-2 text-gray-500">{a.firstName} {a.lastName}</td>
                        <td className="px-4 py-2 text-gray-400">{a.email}</td>
                        <td className="px-4 py-2">
                          <span className={`px-2 py-1 rounded-full text-xs ${a.status === "refunded" ? "bg-red-100 text-red-600" : "bg-amber-100 text-amber-600"}`}>{a.status}</span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </>
      )}

      {/* Cancel dialog */}
      {showCancelDialog && (
        <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg shadow-xl p-6 max-w-lg w-full mx-4">
            <h2 className="text-lg font-bold text-red-700 mb-4">Cancel {courseName}</h2>
            <p className="text-sm text-gray-600 mb-4">Choose an action for each attendee:</p>
            <div className="space-y-2 max-h-60 overflow-y-auto mb-4">
              {active.map((a) => (
                <div key={a.enrolmentId} className="flex items-center justify-between border rounded p-2">
                  <span className="text-sm">{a.firstName} {a.lastName}</span>
                  <select
                    value={cancelActions[a.enrolmentId] || "transfer"}
                    onChange={(e) => setCancelActions({ ...cancelActions, [a.enrolmentId]: e.target.value })}
                    className="border rounded px-2 py-1 text-sm"
                  >
                    <option value="transfer">Transfer</option>
                    <option value="refund">Refund</option>
                  </select>
                </div>
              ))}
            </div>
            <div className="flex gap-2 justify-end">
              <button onClick={() => setShowCancelDialog(false)} className="px-4 py-2 rounded text-sm text-gray-600 hover:bg-gray-100">Back</button>
              <button onClick={cancelCourse} className="bg-red-600 text-white px-4 py-2 rounded text-sm hover:bg-red-700">Cancel Course</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
