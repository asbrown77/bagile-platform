"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { CourseAttendee, getCourseAttendees } from "@/lib/api";

export default function CourseDetail() {
  const params = useParams();
  const courseId = Number(params.id);
  const [attendees, setAttendees] = useState<CourseAttendee[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    const key = localStorage.getItem("bagile_api_key");
    if (!key) {
      window.location.replace("/login");
      return;
    }
    loadAttendees(key);
  }, [courseId]);

  async function loadAttendees(key: string) {
    setLoading(true);
    try {
      const data = await getCourseAttendees(key, courseId);
      setAttendees(data);
    } catch {
      setError("Failed to load attendees");
    } finally {
      setLoading(false);
    }
  }

  function downloadCsv() {
    const key = localStorage.getItem("bagile_api_key");
    if (!key) return;
    const url = `${process.env.NEXT_PUBLIC_API_URL || "https://api.bagile.co.uk"}/api/course-schedules/${courseId}/attendees/export`;
    fetch(url, { headers: { "X-Api-Key": key } })
      .then((r) => r.blob())
      .then((blob) => {
        const a = document.createElement("a");
        a.href = URL.createObjectURL(blob);
        a.download = `attendees-${attendees[0]?.courseCode || courseId}.csv`;
        a.click();
      });
  }

  const courseName = attendees[0]?.courseName || `Course ${courseId}`;
  const courseCode = attendees[0]?.courseCode || "";
  const activeAttendees = attendees.filter((a) => a.status !== "cancelled" && a.status !== "transferred");

  return (
    <div className="max-w-4xl mx-auto py-8 px-4">
      {/* Header */}
      <div className="mb-6">
        <a href="/dashboard" className="text-sm text-blue-600 hover:underline mb-2 inline-block">&larr; Back to Dashboard</a>
        <h1 className="text-2xl font-bold text-gray-900">{courseName}</h1>
        {courseCode && <p className="text-gray-500 text-sm">{courseCode}</p>}
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>
      )}

      {loading ? (
        <p className="text-gray-500">Loading attendees...</p>
      ) : (
        <>
          {/* Summary + export */}
          <div className="flex items-center justify-between mb-4">
            <p className="text-gray-700 font-medium">{activeAttendees.length} attendee{activeAttendees.length !== 1 ? "s" : ""}</p>
            {activeAttendees.length > 0 && (
              <button
                onClick={downloadCsv}
                className="bg-blue-600 text-white px-4 py-2 rounded text-sm hover:bg-blue-700"
              >
                Download CSV
              </button>
            )}
          </div>

          {/* Attendee table */}
          <div className="bg-white rounded-lg shadow-sm border overflow-hidden">
            {activeAttendees.length === 0 ? (
              <p className="p-6 text-gray-500 text-center">No attendees yet. Data may still be importing.</p>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Name</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Email</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Organisation</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Status</th>
                  </tr>
                </thead>
                <tbody>
                  {activeAttendees.map((a) => (
                    <tr key={a.studentId} className="border-t">
                      <td className="px-4 py-3 font-medium text-gray-900">{a.firstName} {a.lastName}</td>
                      <td className="px-4 py-3 text-gray-600">{a.email}</td>
                      <td className="px-4 py-3 text-gray-600">{a.organisation || "—"}</td>
                      <td className="px-4 py-3">
                        <span className="px-2 py-1 rounded-full text-xs font-medium bg-green-100 text-green-700">
                          {a.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>
        </>
      )}
    </div>
  );
}
