"use client";

import { useState } from "react";
import { CourseAttendee, updateStudent } from "@/lib/api";
import { Button } from "@/components/ui/Button";
import { AlertBanner } from "@/components/ui/AlertBanner";
import { X } from "lucide-react";

interface Props {
  attendee: CourseAttendee;
  apiKey: string;
  onSaved: () => void;
  onClose: () => void;
  /** When true, the Company/Organisation field is hidden — irrelevant for private courses
   *  where all attendees are from the same client organisation. */
  isPrivate?: boolean;
}

export function EditAttendeeModal({ attendee, apiKey, onSaved, onClose, isPrivate = false }: Props) {
  const [firstName, setFirstName] = useState(attendee.firstName || "");
  const [lastName, setLastName] = useState(attendee.lastName || "");
  const [email, setEmail] = useState(attendee.email || "");
  const [company, setCompany] = useState(attendee.organisation || "");
  const [overrideNote, setOverrideNote] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function handleSave() {
    setError("");
    setSaving(true);
    try {
      await updateStudent(apiKey, attendee.studentId, {
        email: email !== attendee.email ? email : undefined,
        firstName: firstName !== attendee.firstName ? firstName : undefined,
        lastName: lastName !== attendee.lastName ? lastName : undefined,
        company: company !== (attendee.organisation || "") ? company : undefined,
        updatedBy: "portal",
        overrideNote: overrideNote.trim() || undefined,
      });
      onSaved();
      onClose();
    } catch {
      setError("Failed to save changes — please try again.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center">
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/40" onClick={onClose} />

      {/* Modal */}
      <div className="relative bg-white rounded-xl shadow-xl w-full max-w-md mx-4 p-6 z-10">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h2 className="text-base font-semibold text-gray-900">Edit Attendee</h2>
            <p className="text-xs text-gray-500 mt-0.5">
              Changes are platform-only — no effect on FooEvents tickets or WooCommerce orders.
              Overridden fields survive ETL re-syncs.
            </p>
          </div>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 rounded-lg p-1 hover:bg-gray-100 ml-4">
            <X className="w-4 h-4" />
          </button>
        </div>

        {error && <div className="mb-4"><AlertBanner variant="danger">{error}</AlertBanner></div>}

        <div className="space-y-3">
          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">First Name</label>
              <input
                type="text"
                value={firstName}
                onChange={(e) => setFirstName(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Last Name</label>
              <input
                type="text"
                value={lastName}
                onChange={(e) => setLastName(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
              />
            </div>
          </div>

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm font-mono"
            />
          </div>

          {/* Company field is only relevant for public courses — private attendees
              are all from the same client org so this would be noise. */}
          {!isPrivate && (
            <div>
              <label className="block text-xs font-medium text-gray-700 mb-1">Company / Organisation</label>
              <input
                type="text"
                value={company}
                onChange={(e) => setCompany(e.target.value)}
                className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
              />
            </div>
          )}

          <div>
            <label className="block text-xs font-medium text-gray-700 mb-1">
              Override note (optional)
            </label>
            <input
              type="text"
              value={overrideNote}
              onChange={(e) => setOverrideNote(e.target.value)}
              placeholder="e.g. PTN partner — real attendee at Ofgem"
              className="w-full border border-gray-300 rounded-lg px-3 py-2 text-sm"
            />
          </div>
        </div>

        <div className="flex gap-3 mt-5 pt-4 border-t border-gray-200">
          <Button onClick={handleSave} disabled={saving}>
            {saving ? "Saving..." : "Save Changes"}
          </Button>
          <Button variant="secondary" onClick={onClose}>Cancel</Button>
        </div>
      </div>
    </div>
  );
}
