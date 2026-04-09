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
  const [editedBody, setEditedBody] = useState("");
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
    setEditedBody(template?.htmlBody ?? "");
    setResult(null);
    setTestSentTo(null);
    setError("");

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
  }, [open, apiKey, template]);

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
        htmlBodyOverride: editedBody.trim() || undefined,
      });
      setResult(res);
    } catch (e: any) {
      setError(e?.message?.includes("API error") ? "Server error — check template exists for this course type." : (e?.message || "Failed to send email"));
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
        htmlBodyOverride: editedBody.trim() || undefined,
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
    setEditedBody("");
    setResult(null);
    setTestSentTo(null);
    setError("");
    setCustomEmail("");
    onClose();
  }

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
        <div className="space-y-5">

          {/* Recipient summary */}
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

          {templateMissing && (
            <AlertBanner variant="warning">
              No template found for this course type. Compose your email below or create a template in Settings.
            </AlertBanner>
          )}

          {/* Editable body */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">
              {templateMissing ? "Email body" : "Email body (pre-filled, editable)"}
            </label>
            <textarea
              value={editedBody}
              onChange={(e) => setEditedBody(e.target.value)}
              rows={20}
              placeholder={templateMissing ? "Compose your follow-up email here..." : ""}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
              spellCheck={false}
            />
          </div>

          {/* Send actions */}
          <div className="pt-2 border-t border-gray-200 space-y-3">
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
                  disabled={sendingTest || (!editedBody.trim() && templateMissing)}
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
