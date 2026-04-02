"use client";

import { useEffect, useState } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import {
  CourseScheduleDetail,
  CourseAttendee,
  PostCourseTemplate,
  Trainer,
  sendFollowUpEmail,
  sendFollowUpTestEmail,
  getTrainers,
  SendFollowUpResult,
} from "@/lib/api";
import { FlaskConical, Mail, Users } from "lucide-react";

interface Props {
  open: boolean;
  onClose: () => void;
  apiKey: string;
  course: CourseScheduleDetail;
  attendees: CourseAttendee[];
  template: PostCourseTemplate | null;
  templateMissing: boolean;
}

export function SendFollowUpPanel({
  open,
  onClose,
  apiKey,
  course,
  attendees,
  template,
  templateMissing,
}: Props) {
  const [delayNote, setDelayNote] = useState("");
  const [courseTypeOverride, setCourseTypeOverride] = useState("");
  const [sending, setSending] = useState(false);
  const [sendingTest, setSendingTest] = useState(false);
  const [result, setResult] = useState<SendFollowUpResult | null>(null);
  const [testSentTo, setTestSentTo] = useState<string | null>(null);
  const [error, setError] = useState("");

  // Test recipient selection
  const [trainers, setTrainers] = useState<Trainer[]>([]);
  const [testRecipient, setTestRecipient] = useState<string>("");
  const [customEmail, setCustomEmail] = useState("");
  const CUSTOM_VALUE = "__custom__";
  const INFO_EMAIL = "info@bagile.co.uk";

  useEffect(() => {
    if (!open || !apiKey) return;
    getTrainers(apiKey)
      .then((list) => {
        const active = list.filter((t) => t.isActive);
        setTrainers(active);
        const match = active.find(
          (t) => course.trainerName &&
            t.name.trim().toLowerCase() === course.trainerName.trim().toLowerCase()
        );
        setTestRecipient(match?.email ?? active[0]?.email ?? INFO_EMAIL);
      })
      .catch(() => setTestRecipient(INFO_EMAIL));
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, apiKey]);

  const activeAttendees = attendees.filter((a) => a.status === "active");

  async function handleSend() {
    if (activeAttendees.length === 0) {
      setError("No active attendees on this course — nothing to send.");
      return;
    }
    setError("");
    setSending(true);
    try {
      const res = await sendFollowUpEmail(apiKey, course.id, {
        courseTypeOverride: courseTypeOverride.trim() || undefined,
        delayNote: delayNote.trim() || undefined,
      });
      setResult(res);
    } catch (e: any) {
      const msg = e?.message || "Failed to send email";
      // Try to extract API error body
      setError(msg.includes("API error") ? "Server error — check template exists for this course type." : msg);
    } finally {
      setSending(false);
    }
  }

  async function handleSendTest() {
    setError("");
    setTestSentTo(null);
    setSendingTest(true);
    const recipientEmail = testRecipient === CUSTOM_VALUE ? customEmail.trim() : testRecipient;
    if (!recipientEmail) {
      setError("Enter a custom email address for the test send.");
      setSendingTest(false);
      return;
    }
    try {
      const res = await sendFollowUpTestEmail(apiKey, course.id, {
        courseTypeOverride: courseTypeOverride.trim() || undefined,
        recipientEmail,
      });
      setTestSentTo(res.recipientEmail);
    } catch (e: any) {
      setError(e?.message?.includes("API error") ? "Server error — check template exists for this course type." : (e?.message || "Failed to send test email"));
    } finally {
      setSendingTest(false);
    }
  }

  function handleClose() {
    setDelayNote("");
    setCourseTypeOverride("");
    setResult(null);
    setTestSentTo(null);
    setError("");
    setCustomEmail("");
    onClose();
  }

  const derivedCourseType = courseTypeOverride.trim().toUpperCase() ||
    (course.courseCode?.split("-")[0]?.toUpperCase() ?? "");

  return (
    <SlideOver open={open} onClose={handleClose} title="Send Follow-Up Email" subtitle={course.title} wide>
      {error && (
        <div className="mb-4">
          <AlertBanner variant="danger" onDismiss={() => setError("")}>{error}</AlertBanner>
        </div>
      )}

      {result ? (
        <div className="space-y-4">
          <AlertBanner variant="success">
            <p className="font-medium">
              Follow-up email sent to {result.recipientCount} attendee{result.recipientCount !== 1 ? "s" : ""}
            </p>
            <p className="text-sm mt-1">Subject: {result.subject}</p>
            <p className="text-sm text-gray-500 mt-1">info@bagile.co.uk CC'd automatically.</p>
          </AlertBanner>
          <div className="bg-gray-50 rounded-lg border border-gray-200 p-3">
            <p className="text-xs font-medium text-gray-600 mb-2">Sent to:</p>
            {result.recipientEmails.map((email) => (
              <p key={email} className="text-xs text-gray-700">{email}</p>
            ))}
          </div>
          <Button onClick={handleClose}>Done</Button>
        </div>
      ) : (
        <div className="space-y-6">
          {/* Recipient summary */}
          <div className="bg-gray-50 rounded-lg border border-gray-200 p-4">
            <div className="flex items-center gap-2 text-sm font-medium text-gray-700 mb-3">
              <Users className="w-4 h-4 text-gray-400" />
              Recipients ({activeAttendees.length} active attendees)
            </div>
            {activeAttendees.length === 0 ? (
              <p className="text-sm text-amber-600">No active attendees — email cannot be sent.</p>
            ) : (
              <div className="space-y-1">
                {activeAttendees.map((a) => (
                  <p key={a.enrolmentId} className="text-xs text-gray-600">
                    {a.firstName} {a.lastName} &lt;{a.email}&gt;
                  </p>
                ))}
                <p className="text-xs text-gray-400 pt-1">+ info@bagile.co.uk (CC)</p>
              </div>
            )}
          </div>

          {/* Template status */}
          {templateMissing ? (
            <AlertBanner variant="warning">
              No template found for course type <strong>{derivedCourseType}</strong>.
              Create one via Settings &rarr; Templates, or use a course type override below.
            </AlertBanner>
          ) : template ? (
            <div>
              <p className="text-xs font-medium text-gray-700 mb-1">
                Template: <span className="font-mono text-brand-600">{template.courseType}</span>
              </p>
              <p className="text-xs text-gray-500 mb-2">Subject: {template.subjectTemplate}</p>
              <details className="text-xs">
                <summary className="cursor-pointer text-brand-600 hover:text-brand-700 font-medium">
                  Preview email body
                </summary>
                <div
                  className="mt-3 bg-white border border-gray-200 rounded-lg p-4 text-gray-700 prose prose-sm max-w-none overflow-auto max-h-80"
                  dangerouslySetInnerHTML={{ __html: template.htmlBody }}
                />
              </details>
            </div>
          ) : null}

          {/* Optional: delay note */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Delay note (optional)
            </label>
            <input
              type="text"
              value={delayNote}
              onChange={(e) => setDelayNote(e.target.value)}
              placeholder="e.g. Apologies for the slight delay getting these over!"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
            <p className="text-xs text-gray-400 mt-1">
              Inserts as a paragraph at the top. Leave blank if sending promptly.
            </p>
          </div>

          {/* Course type override */}
          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Course type override (optional)
            </label>
            <input
              type="text"
              value={courseTypeOverride}
              onChange={(e) => setCourseTypeOverride(e.target.value.toUpperCase())}
              placeholder={`Detected: ${derivedCourseType || "—"}`}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono uppercase"
              maxLength={20}
            />
            <p className="text-xs text-gray-400 mt-1">
              Use if the course code prefix doesn&apos;t match a template (e.g. PSMAI → PSMO-AI).
            </p>
          </div>

          {/* Send buttons */}
          <div className="pt-2 border-t border-gray-200 space-y-3">
            {/* Test send — dropdown + button */}
            <div>
              <p className="text-xs font-medium text-gray-600 mb-1.5">Send test to:</p>
              <div className="flex items-center gap-2 flex-wrap">
                <select
                  value={testRecipient}
                  onChange={(e) => setTestRecipient(e.target.value)}
                  className="border border-gray-300 rounded-lg px-2.5 py-1.5 text-sm text-gray-700 bg-white"
                >
                  {trainers.map((t) => (
                    <option key={t.email} value={t.email}>{t.name} ({t.email})</option>
                  ))}
                  <option value={INFO_EMAIL}>info@bagile.co.uk</option>
                  <option value={CUSTOM_VALUE}>Custom…</option>
                </select>
                {testRecipient === CUSTOM_VALUE && (
                  <input
                    type="email"
                    value={customEmail}
                    onChange={(e) => setCustomEmail(e.target.value)}
                    placeholder="email@example.com"
                    className="border border-gray-300 rounded-lg px-2.5 py-1.5 text-sm w-52"
                  />
                )}
                <button
                  type="button"
                  onClick={handleSendTest}
                  disabled={sendingTest || templateMissing}
                  className="inline-flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-lg border border-gray-300 text-gray-700 hover:bg-gray-50 disabled:opacity-40 disabled:cursor-not-allowed"
                >
                  <FlaskConical className="w-3.5 h-3.5" />
                  {sendingTest ? "Sending…" : "Send Test"}
                </button>
              </div>
              {testSentTo && (
                <p className="text-xs text-green-700 bg-green-50 border border-green-200 rounded px-2 py-0.5 mt-2 inline-block">
                  Test sent to {testSentTo} — check inbox
                </p>
              )}
            </div>

            {/* Real send */}
            <div className="flex gap-3">
              <Button
                onClick={handleSend}
                disabled={sending || activeAttendees.length === 0}
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
