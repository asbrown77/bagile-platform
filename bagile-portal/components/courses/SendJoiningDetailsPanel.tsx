"use client";

import { useEffect, useState } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import {
  CourseScheduleDetail,
  CourseAttendee,
  PreCourseTemplate,
  getPreCourseTemplate,
  sendPreCourseEmail,
  sendPreCourseTestEmail,
} from "@/lib/api";
import { Code, Eye, FlaskConical, Mail, Settings, Users } from "lucide-react";
import Link from "next/link";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  course: CourseScheduleDetail;
  attendees: CourseAttendee[];
}

/** Derive the format string the backend expects based on course formatType. */
function deriveFormat(course: CourseScheduleDetail): string {
  return course.formatType?.toLowerCase().includes("virtual") ? "Virtual" : "F2F";
}

/** Derive the course type prefix from course code (e.g. "PSM-270426-AB" → "PSM"). */
function deriveCourseType(course: CourseScheduleDetail): string {
  return course.courseCode?.split("-")[0]?.toUpperCase() ?? "";
}

/**
 * Replace template variables in the HTML body with values from the course record.
 * Variables that have no value are left as-is so the trainer can see what needs filling.
 */
function applyVariables(html: string, course: CourseScheduleDetail): string {
  const formatDate = (d: string | null | undefined) => {
    if (!d) return "";
    return new Date(d).toLocaleDateString("en-GB", { weekday: "long", day: "numeric", month: "long", year: "numeric" });
  };

  const startFmt = formatDate(course.startDate);
  const endFmt = course.endDate && course.endDate !== course.startDate ? formatDate(course.endDate) : "";
  const dates = endFmt ? `${startFmt} and ${endFmt}` : startFmt;

  const replacements: Record<string, string> = {
    "{{course_name}}": course.title ?? "",
    "{{dates}}": dates,
    "{{times}}": "",          // not stored on course record — trainer fills in
    "{{trainer_name}}": course.trainerName ?? "",
    "{{venue_address}}": course.venueAddress ?? "",
    "{{zoom_url}}": course.meetingUrl ?? "",
    "{{zoom_id}}": course.meetingId ?? "",
    "{{zoom_passcode}}": course.meetingPasscode ?? "",
  };

  let result = html;
  for (const [key, value] of Object.entries(replacements)) {
    result = result.replaceAll(key, value);
  }
  return result;
}

export function SendJoiningDetailsPanel({ open, onClose, apiKey, course, attendees }: Props) {
  const [template, setTemplate] = useState<PreCourseTemplate | null>(null);
  const [templateMissing, setTemplateMissing] = useState(false);
  const [loadingTemplate, setLoadingTemplate] = useState(false);

  // Editable body — pre-filled from template with variables substituted
  const [editedBody, setEditedBody] = useState("");
  const [showPreview, setShowPreview] = useState(false);

  const [sending, setSending] = useState(false);
  const [sendingTest, setSendingTest] = useState(false);
  const [sentCount, setSentCount] = useState<number | null>(null);
  const [testSentTo, setTestSentTo] = useState<string | null>(null);
  const [error, setError] = useState("");

  const courseType = deriveCourseType(course);
  const format = deriveFormat(course);
  const isVirtual = format === "Virtual";

  const activeAttendees = attendees.filter((a) => a.status === "active");

  // Missing venue/zoom warning
  const missingDetails = isVirtual
    ? !course.meetingUrl
    : !course.venueAddress;

  useEffect(() => {
    if (!open || !apiKey || !courseType) return;
    setLoadingTemplate(true);
    setTemplate(null);
    setTemplateMissing(false);
    setEditedBody("");
    setSentCount(null);
    setTestSentTo(null);
    setError("");

    getPreCourseTemplate(apiKey, courseType, format)
      .then((t) => {
        setTemplate(t);
        setTemplateMissing(false);
        setEditedBody(applyVariables(t.htmlBody, course));
      })
      .catch(() => {
        setTemplate(null);
        setTemplateMissing(true);
        setEditedBody("");
      })
      .finally(() => setLoadingTemplate(false));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, apiKey, courseType, format]);

  async function handleSend() {
    if (activeAttendees.length === 0) {
      setError("No active attendees on this course — nothing to send.");
      return;
    }
    setError("");
    setSending(true);
    try {
      const res = await sendPreCourseEmail(apiKey, course.id, {
        htmlBodyOverride: editedBody || undefined,
      });
      setSentCount(res.recipientCount);
    } catch (e: any) {
      setError(e?.message?.includes("API error") ? "Server error — check the template and course details." : (e?.message || "Failed to send email"));
    } finally {
      setSending(false);
    }
  }

  async function handleSendTest() {
    setError("");
    setTestSentTo(null);
    setSendingTest(true);
    try {
      const res = await sendPreCourseTestEmail(apiKey, course.id, {
        htmlBodyOverride: editedBody || undefined,
      });
      setTestSentTo(res.sentTo);
    } catch (e: any) {
      setError(e?.message?.includes("API error") ? "Server error — check the template exists for this course type." : (e?.message || "Failed to send test email"));
    } finally {
      setSendingTest(false);
    }
  }

  function handleClose() {
    setTemplate(null);
    setTemplateMissing(false);
    setEditedBody("");
    setShowPreview(false);
    setSentCount(null);
    setTestSentTo(null);
    setError("");
    onClose();
  }

  return (
    <SlideOver open={open} onClose={handleClose} title="Send Joining Details" subtitle={course.title} wide>
      {error && (
        <div className="mb-4">
          <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>
        </div>
      )}

      {/* ── Success screen ── */}
      {sentCount !== null ? (
        <div className="space-y-4">
          <AlertBanner variant="success">
            <p className="font-medium">
              Joining details sent to {sentCount} attendee{sentCount !== 1 ? "s" : ""}
            </p>
            <p className="text-sm text-gray-500 mt-1">info@bagile.co.uk CC&apos;d automatically.</p>
          </AlertBanner>
          <Button onClick={handleClose}>Done</Button>
        </div>
      ) : (
        <div className="space-y-5">

          {/* ── Missing venue/zoom warning ── */}
          {missingDetails && (
            <AlertBanner variant="warning">
              {isVirtual
                ? "No Zoom meeting URL set on this course — attendees won't have a join link."
                : "No venue address set on this course — attendees won't have a location."}
              {" "}
              <Link href={`/courses/${course.id}`} className="underline font-medium">
                Edit course details
              </Link>{" "}
              to add {isVirtual ? "Zoom details" : "the venue address"} before sending.
            </AlertBanner>
          )}

          {/* ── No template warning ── */}
          {templateMissing && !loadingTemplate && (
            <AlertBanner variant="warning">
              No pre-course template found for <strong>{courseType}</strong> ({format}).{" "}
              <Link href="/settings" className="underline font-medium inline-flex items-center gap-1">
                <Settings className="w-3 h-3" /> Create one in Settings
              </Link>{" "}
              or compose your email below from scratch.
            </AlertBanner>
          )}

          {/* ── Recipients ── */}
          <div className="bg-gray-50 rounded-lg border border-gray-200 p-4">
            <div className="flex items-center gap-2 text-sm font-medium text-gray-700 mb-3">
              <Users className="w-4 h-4 text-gray-400" />
              Recipients ({activeAttendees.length} active attendee{activeAttendees.length !== 1 ? "s" : ""})
            </div>
            {activeAttendees.length === 0 ? (
              <p className="text-sm text-amber-600">No active attendees — email cannot be sent.</p>
            ) : (
              <div className="space-y-1">
                {activeAttendees.slice(0, 8).map((a) => (
                  <p key={a.enrolmentId} className="text-xs text-gray-600">
                    {a.firstName} {a.lastName} &lt;{a.email}&gt;
                  </p>
                ))}
                {activeAttendees.length > 8 && (
                  <p className="text-xs text-gray-400">+ {activeAttendees.length - 8} more</p>
                )}
                <p className="text-xs text-gray-400 pt-1">+ info@bagile.co.uk (CC)</p>
              </div>
            )}
          </div>

          {/* ── HTML editor with Source / Preview toggle ── */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-sm font-medium text-gray-700">
                {loadingTemplate ? "Loading template..." : template ? "Email body (pre-filled, editable)" : "Email body"}
              </label>
              {/* Source / Preview toggle — only show once we have something to preview */}
              {(template || editedBody) && (
                <div className="flex rounded-lg border border-gray-200 overflow-hidden">
                  <button
                    type="button"
                    onClick={() => setShowPreview(false)}
                    className={`flex items-center gap-1 px-2.5 py-1 text-xs font-medium transition-colors
                      ${!showPreview ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
                  >
                    <Code className="w-3 h-3" /> Source
                  </button>
                  <button
                    type="button"
                    onClick={() => setShowPreview(true)}
                    className={`flex items-center gap-1 px-2.5 py-1 text-xs font-medium border-l border-gray-200 transition-colors
                      ${showPreview ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
                  >
                    <Eye className="w-3 h-3" /> Preview
                  </button>
                </div>
              )}
            </div>

            {loadingTemplate ? (
              <div className="border border-gray-200 rounded-lg h-40 flex items-center justify-center text-sm text-gray-400">
                Loading template...
              </div>
            ) : !showPreview ? (
              <textarea
                value={editedBody}
                onChange={(e) => setEditedBody(e.target.value)}
                rows={20}
                placeholder={templateMissing ? "Compose your joining details email here..." : ""}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
                spellCheck={false}
              />
            ) : (
              <div className="border border-gray-300 rounded-lg overflow-auto bg-white" style={{ minHeight: "20rem" }}>
                {editedBody.trim() ? (
                  <iframe
                    srcDoc={editedBody}
                    className="w-full border-0"
                    style={{ minHeight: "20rem" }}
                    title="Email preview"
                    sandbox="allow-same-origin"
                  />
                ) : (
                  <div className="flex items-center justify-center h-48 text-sm text-gray-400">
                    Nothing to preview — add HTML in Source mode.
                  </div>
                )}
              </div>
            )}

            {!templateMissing && (
              <p className="text-xs text-gray-400 mt-1">
                Variables:
                {" "}<code>{"{{course_name}}"}</code>,
                {" "}<code>{"{{dates}}"}</code>,
                {" "}<code>{"{{times}}"}</code>,
                {" "}<code>{"{{trainer_name}}"}</code>,
                {" "}<code>{"{{venue_address}}"}</code>,
                {" "}<code>{"{{zoom_url}}"}</code>,
                {" "}<code>{"{{zoom_id}}"}</code>,
                {" "}<code>{"{{zoom_passcode}}"}</code>
              </p>
            )}
          </div>

          {/* ── Send actions ── */}
          <div className="pt-2 border-t border-gray-200 space-y-3">
            {/* Test send */}
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={handleSendTest}
                disabled={sendingTest || !editedBody.trim()}
                className="inline-flex items-center gap-1.5 text-sm text-brand-600 hover:text-brand-700 disabled:opacity-40 disabled:cursor-not-allowed font-medium"
              >
                <FlaskConical className="w-3.5 h-3.5" />
                {sendingTest ? "Sending test..." : "Send test to me"}
              </button>
              {testSentTo && (
                <span className="text-xs text-green-700 bg-green-50 border border-green-200 rounded px-2 py-0.5">
                  Test sent to {testSentTo}
                </span>
              )}
            </div>

            {/* Real send */}
            <div className="flex gap-3">
              <Button
                onClick={handleSend}
                disabled={sending || activeAttendees.length === 0 || !editedBody.trim()}
              >
                <Mail className="w-3.5 h-3.5" />
                {sending ? "Sending..." : `Send to ${activeAttendees.length} attendee${activeAttendees.length !== 1 ? "s" : ""}`}
              </Button>
              <Button variant="secondary" onClick={handleClose}>Cancel</Button>
            </div>
          </div>
        </div>
      )}
    </SlideOver>
  );
}
