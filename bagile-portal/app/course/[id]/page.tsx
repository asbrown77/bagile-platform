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

  useEffect(() => {
    if (!API_KEY) { setError("API key not configured"); setLoading(false); return; }
    loadAttendees();
  }, [courseId]);

  async function loadAttendees() {
    try {
      setAttendees(await getCourseAttendees(API_KEY, courseId));
    } catch {
      setError("Failed to load attendees");
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
        const dateStr = startDate ? new Date(startDate).toLocaleDateString("en-GB", { day: "2-digit", month: "2-digit", year: "2-digit" }).replace(/\//g, "") : courseId.toString();
        a.download = `${code}-Students-${dateStr}.csv`;
        a.click();
      });
  }

  function emailAll() {
    const emails = activeAttendees.map((a) => a.email).join(",");
    window.open(`mailto:${emails}?subject=${encodeURIComponent(courseName)}`);
  }

  function emailOne(email: string, name: string) {
    window.open(`mailto:${email}?subject=${encodeURIComponent(courseName)}`);
  }

  const courseName = attendees[0]?.courseName || `Course ${courseId}`;
  const courseCode = attendees[0]?.courseCode || "";
  const startDate = attendees[0]?.courseName?.match(/\d{1,2}[-\s]\w{3}\s\d{2}/)?.[0] || "";
  const activeAttendees = attendees.filter((a) => a.status !== "cancelled" && a.status !== "transferred");

  return (
    <div className="max-w-5xl mx-auto py-8 px-4">
      {/* Nav */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <a href="/dashboard" className="text-sm text-blue-600 hover:underline mb-1 inline-block">&larr; Dashboard</a>
          <h1 className="text-2xl font-bold text-gray-900">{courseName}</h1>
          {courseCode && <p className="text-gray-500 text-sm">{courseCode}</p>}
        </div>
        <div className="flex gap-2">
          {activeAttendees.length > 0 && (
            <>
              <button onClick={downloadCsv} className="bg-blue-600 text-white px-3 py-2 rounded text-sm hover:bg-blue-700">
                Export CSV
              </button>
              <button onClick={emailAll} className="bg-gray-600 text-white px-3 py-2 rounded text-sm hover:bg-gray-700">
                Email All
              </button>
            </>
          )}
        </div>
      </div>

      {error && <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">{error}</div>}

      {loading ? (
        <p className="text-gray-500">Loading attendees...</p>
      ) : (
        <>
          <p className="text-gray-600 text-sm mb-4">{activeAttendees.length} attendee{activeAttendees.length !== 1 ? "s" : ""}</p>

          <div className="bg-white rounded-lg shadow-sm border overflow-hidden">
            {activeAttendees.length === 0 ? (
              <p className="p-6 text-gray-500 text-center">No attendees yet.</p>
            ) : (
              <table className="w-full text-sm">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Name</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Email</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Organisation</th>
                    <th className="text-left px-4 py-2 font-medium text-gray-600">Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {activeAttendees.map((a) => (
                    <tr key={a.studentId} className="border-t hover:bg-gray-50">
                      <td className="px-4 py-3 font-medium text-gray-900">{a.firstName} {a.lastName}</td>
                      <td className="px-4 py-3">
                        <a href={`mailto:${a.email}?subject=${encodeURIComponent(courseName)}`} className="text-blue-600 hover:underline">
                          {a.email}
                        </a>
                      </td>
                      <td className="px-4 py-3 text-gray-600">{a.organisation || "—"}</td>
                      <td className="px-4 py-3">
                        <button onClick={() => emailOne(a.email, `${a.firstName} ${a.lastName}`)} className="text-blue-600 hover:text-blue-800 text-xs mr-3">
                          Email
                        </button>
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
