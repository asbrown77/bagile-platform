"use client";

import { EmailSendLog } from "@/lib/api";
import { ClipboardList, FileText } from "lucide-react";

interface EmailSectionProps {
  emailLog: EmailSendLog[];
  logLoading: boolean;
  hasAttendees: boolean;
  onOpenJoining: () => void;
  onOpenFollowUp: () => void;
}

/** Format a send log entry as a human-readable status line. */
function formatLogStatus(entries: EmailSendLog[], templateType: string): string {
  const matches = entries
    .filter((e) => e.templateType === templateType)
    .sort((a, b) => new Date(b.sentAt).getTime() - new Date(a.sentAt).getTime());

  if (matches.length === 0) return "Not yet sent";

  const latest = matches[0];
  const date = new Date(latest.sentAt).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
  });

  if (latest.isTest) return `Test sent ${date}`;
  return `Sent ${date} (${latest.recipientCount} recipient${latest.recipientCount !== 1 ? "s" : ""})`;
}

interface EmailCardProps {
  icon: React.ReactNode;
  label: string;
  status: string;
  isTest: boolean;
  neverSent: boolean;
  disabled: boolean;
  onClick: () => void;
}

function EmailCard({ icon, label, status, isTest, neverSent, disabled, onClick }: EmailCardProps) {
  const statusColour = neverSent
    ? "text-gray-400"
    : isTest
    ? "text-amber-600"
    : "text-green-700";

  return (
    <div className="bg-white rounded-xl border border-gray-200 shadow-sm p-4 flex flex-col gap-3">
      <div className="flex items-center gap-2">
        <span className="text-gray-500">{icon}</span>
        <span className="text-sm font-semibold text-gray-800">{label}</span>
      </div>
      <p className={`text-xs font-medium ${statusColour}`}>{status}</p>
      <button
        onClick={onClick}
        disabled={disabled}
        className="mt-auto inline-flex items-center justify-center gap-1.5 px-3 py-1.5 text-sm font-medium rounded-lg border border-brand-600 text-brand-600 hover:bg-brand-50 disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
      >
        Preview &amp; Send
      </button>
    </div>
  );
}

export function EmailSection({
  emailLog,
  logLoading,
  hasAttendees,
  onOpenJoining,
  onOpenFollowUp,
}: EmailSectionProps) {
  const joiningEntries = emailLog.filter((e) => e.templateType === "pre_course");
  const followUpEntries = emailLog.filter((e) => e.templateType === "post_course");

  const joiningStatus = logLoading ? "Loading…" : formatLogStatus(emailLog, "pre_course");
  const followUpStatus = logLoading ? "Loading…" : formatLogStatus(emailLog, "post_course");

  const joiningNeverSent = !logLoading && joiningEntries.length === 0;
  const followUpNeverSent = !logLoading && followUpEntries.length === 0;

  const joiningLatestIsTest =
    joiningEntries.length > 0 &&
    [...joiningEntries].sort((a, b) => new Date(b.sentAt).getTime() - new Date(a.sentAt).getTime())[0].isTest;

  const followUpLatestIsTest =
    followUpEntries.length > 0 &&
    [...followUpEntries].sort((a, b) => new Date(b.sentAt).getTime() - new Date(a.sentAt).getTime())[0].isTest;

  return (
    <div className="mb-6">
      <h2 className="text-xs font-semibold text-gray-400 uppercase tracking-wide mb-3">Emails</h2>
      <div className="grid grid-cols-2 gap-4">
        <EmailCard
          icon={<ClipboardList className="w-4 h-4" />}
          label="Joining Details"
          status={joiningStatus}
          isTest={joiningLatestIsTest}
          neverSent={joiningNeverSent}
          disabled={!hasAttendees}
          onClick={onOpenJoining}
        />
        <EmailCard
          icon={<FileText className="w-4 h-4" />}
          label="Follow-Up"
          status={followUpStatus}
          isTest={followUpLatestIsTest}
          neverSent={followUpNeverSent}
          disabled={!hasAttendees}
          onClick={onOpenFollowUp}
        />
      </div>
    </div>
  );
}
