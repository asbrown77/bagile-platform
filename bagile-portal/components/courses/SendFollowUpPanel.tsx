"use client";

import { useEffect, useState } from "react";
import { SlideOver } from "@/components/ui/SlideOver";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import {
  CourseScheduleDetail,
  CourseAttendee,
  CourseContact,
  PostCourseTemplate,
  Trainer,
  sendFollowUpEmail,
  sendFollowUpTestEmail,
  getFollowUpEmailPreview,
  getCourseContacts,
  getTrainers,
  SendFollowUpResult,
} from "@/lib/api";
import { Code, Eye, FlaskConical, Mail, Users } from "lucide-react";

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
  const [contacts, setContacts] = useState<CourseContact[]>([]);
  const [ccContactIds, setCcContactIds] = useState<Set<number>>(new Set());
  const [showPreview, setShowPreview] = useState(false);
  const [previewHtml, setPreviewHtml] = useState<string | null>(null);
  const [loadingPreview, setLoadingPreview] = useState(false);
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
    setShowPreview(false);
    setPreviewHtml(null);
    setResult(null);
    setTestSentTo(null);
    setError("");
    setContacts([]);
    setCcContactIds(new Set());

    getCourseContacts(apiKey, course.id).then((list) => {
      setContacts(list);
      setCcContactIds(new Set(list.filter((c) => c.role === "organiser").map((c) => c.id)));
    }).catch(() => {});

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
      const additionalCc = contacts
        .filter((c) => ccContactIds.has(c.id) && c.email)
        .map((c) => c.email);
      const res = await sendFollowUpEmail(apiKey, course.id, {
        htmlBodyOverride: editedBody.trim() || undefined,
        additionalCc: additionalCc.length > 0 ? additionalCc : undefined,
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
    setContacts([]);
    setCcContactIds(new Set());
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

          {/* ── CC contacts ── */}
          {contacts.length > 0 && (
            <div>
              <p className="text-xs font-medium text-gray-700 mb-1.5">CC on this email:</p>
              <div className="space-y-1">
                {contacts.map((c) => (
                  <label key={c.id} className="flex items-center gap-2 cursor-pointer group">
                    <input
                      type="checkbox"
                      checked={ccContactIds.has(c.id)}
                      onChange={(e) => {
                        setCcContactIds((prev) => {
                          const next = new Set(prev);
                          e.target.checked ? next.add(c.id) : next.delete(c.id);
                          return next;
                        });
                      }}
                      className="rounded border-gray-300 text-brand-600 focus:ring-brand-500"
                    />
                    <span className="text-sm text-gray-700 group-hover:text-gray-900">
                      {c.name}
                      <span className="text-gray-400 ml-1">({c.email})</span>
                      <span className={`ml-1.5 text-xs font-medium px-1.5 py-0.5 rounded-full ${c.role === "organiser" ? "bg-blue-100 text-blue-700" : "bg-gray-100 text-gray-600"}`}>
                        {c.role}
                      </span>
                    </span>
                  </label>
                ))}
              </div>
            </div>
          )}

          {/* Editable body with Source/Preview toggle */}
          <div>
            <div className="flex items-center justify-between mb-1">
              <label className="block text-sm font-medium text-gray-700">
                {templateMissing ? "Email body" : "Email body (pre-filled, editable)"}
              </label>
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
                    onClick={async () => {
                      setShowPreview(true);
                      setLoadingPreview(true);
                      setPreviewHtml(null);
                      try {
                        const html = await getFollowUpEmailPreview(apiKey, course.id, editedBody || undefined);
                        setPreviewHtml(html);
                      } catch {
                        setPreviewHtml("<p style='padding:16px;color:red'>Failed to load preview</p>");
                      } finally {
                        setLoadingPreview(false);
                      }
                    }}
                    className={`flex items-center gap-1 px-2.5 py-1 text-xs font-medium border-l border-gray-200 transition-colors
                      ${showPreview ? "bg-brand-600 text-white" : "bg-white text-gray-600 hover:bg-gray-50"}`}
                  >
                    <Eye className="w-3 h-3" /> {loadingPreview ? "Loading…" : "Preview"}
                  </button>
                </div>
              )}
            </div>

            {!showPreview ? (
              <textarea
                value={editedBody}
                onChange={(e) => setEditedBody(e.target.value)}
                rows={20}
                placeholder={templateMissing ? "Compose your follow-up email here..." : ""}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono focus:ring-2 focus:ring-brand-500 focus:border-brand-500"
                spellCheck={false}
              />
            ) : (
              <div className="border border-gray-300 rounded-lg overflow-hidden bg-white" style={{ minHeight: "24rem" }}>
                {loadingPreview ? (
                  <div className="flex items-center justify-center h-48 text-sm text-gray-400">Loading preview…</div>
                ) : previewHtml ? (
                  <iframe
                    srcDoc={previewHtml}
                    className="w-full border-0"
                    style={{ minHeight: "24rem", height: "600px" }}
                    title="Email preview"
                    sandbox="allow-same-origin"
                  />
                ) : (
                  <div className="flex items-center justify-center h-48 text-sm text-gray-400">
                    Nothing to preview.
                  </div>
                )}
              </div>
            )}
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
